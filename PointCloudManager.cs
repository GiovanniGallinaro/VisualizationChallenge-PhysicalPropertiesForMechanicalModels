// Original project by Gerard Llorach (2014)
// Updated by Oliver Dawkins and Dominic Zisch (2017) to visualise points using height and intensity gradients
//modified by Simone Milani 2020 for 3DAR 20/21

#pragma warning disable 0168 

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine.UI;

public class PointCloudManager : MonoBehaviour {

    // File location
    public string dataPath;
    private string filename;
    public Material matVertex;

    // Methods to colour points
    public enum cpb { Default, RGB, Height, Intensity };
    public cpb colourPointsBy = cpb.RGB;
    public Color defaultPointColour;
    public Gradient colourGradient;

    // Processing GUI
    private float progress = 0;
    private new string guiText;
    private bool loaded = false;

    // Point cloud properties
    private GameObject pointCloud;
    public GameObject car;
    public Camera myCam;

	public float scale = 1;
    public bool relocateToOrigin = false;
    public bool invertYZ = false;
	public bool forceReload = false;
    private bool restart;

    private bool play;

	public int numPoints;
	public int numPointGroups;
	//private int limitPoints = 603000;
    private int groupPoints;
    private int indexIt = 0;

	private Vector3[] points;
    private Color[] colors;
	private Color[] defaultColors;
    private Color[] inputColors;
    private Color[] heightColors;
	private Vector3 minValue;
    private Vector3[] centers;

    // Point height properties
    public float minHeight;
    public float maxHeight;
    private float heightDiff;
    private float localDiff;

    // Point intensity properties
    public float minIntensity;
    public float maxIntensity;
    private float intensityDiff;
    private float relativeDiff;
    private string currentDirectory;
	private string currentBaseDirectory;

    void Start () {

        minHeight = -0.07f;
        maxHeight = 1.184f;
        heightDiff = maxHeight - minHeight;

        //Calculate intensity difference for visualising intensity gradient
        intensityDiff = maxIntensity - minIntensity;

	    currentDirectory = Application.dataPath;
        currentDirectory = currentDirectory.Replace( @"\", "/" );
        bool hasFoundMatch = false;

        if ( !currentDirectory.EndsWith( "/" ) )
            currentDirectory += "/";

        switch (Application.platform) {
            case RuntimePlatform.OSXEditor: //<path to project folder>/Assets
            case RuntimePlatform.WindowsEditor:
                if(currentDirectory.EndsWith("Assets/")) {
                    currentDirectory = currentDirectory.Substring(0, currentDirectory.LastIndexOf( "Assets/" ) );
                    currentDirectory += "RuntimeData/";
                    hasFoundMatch = true;
                }
                break;
            case RuntimePlatform.WindowsPlayer: //<path to executablename_Data folder>
                break;
            case RuntimePlatform.OSXPlayer: //<path to player app bundle>/Contents
                if(currentDirectory.EndsWith(".app/Contents/")) {
                    currentDirectory = currentDirectory.Substring(0, currentDirectory.LastIndexOf( ".app/Contents/" ) );
                    currentDirectory += "RuntimeData/";
                    hasFoundMatch = true;
                }
                break;
            default:
                hasFoundMatch = false;
                break;
        }

	Debug.Log(currentDirectory);

	if (!hasFoundMatch) {
            currentDirectory = Path.GetFullPath("RuntimeData/");
            currentDirectory = currentDirectory.Replace(@"\", "/");
        }

    if (!Directory.Exists( currentDirectory)) {
            for (int i = 0; i < 2; i++)
                currentDirectory = currentDirectory.Substring( 0, currentDirectory.LastIndexOf( "/" ) );
            currentDirectory += "/RuntimeData/";
        }

    currentBaseDirectory = currentDirectory.Replace("/RuntimeData", "");
	Debug.Log(currentBaseDirectory);

    var lines = File.ReadAllLines(currentBaseDirectory+ "/Assets/Models/" + dataPath + ".xyz");

	Debug.Log("All fine");

    // Create Resources folder
    createFolders();

	// Get Filename
	filename = Path.GetFileName(currentBaseDirectory + "/Assets/Models/" +dataPath + ".xyz");
	Debug.Log(filename);

    centers = new Vector3[numPointGroups];
	loadPointCloud();

    play = true;
    restart = false;

    }

    void Update(){

        if (loaded){
            Mesh mesh = car.transform.GetComponent<MeshFilter>().mesh;
            pointCloud.transform.position = car.transform.position + new Vector3(-mesh.bounds.size.x / 10 + 0.35f, -0.075f, -mesh.bounds.size.x / 4.5f + 0.06f);
            //pointCloud.transform.position = car.transform.position;
            pointCloud.transform.rotation = car.transform.rotation;

            playFrame();
        }

    }

    // change color according to the dropdown menu
    public void retrieveColor(int colorInd)
    {
        if (loaded && !play)
        {
            for (int id = 0; id < numPointGroups; id++)
            {
                // get current mesh
                GameObject currChild = GameObject.Find("/GameObject/flows_ata.xyz/flows_ata.xyz" + id.ToString() + "T/flows_ata.xyz" + id.ToString());
                Mesh mesh = currChild.GetComponent<MeshFilter>().mesh;

                // get the color according to the input choice
                if (colorInd == 0)
                    colors = defaultColors;
                else if (colorInd == 1)
                    colors = inputColors;
                else
                    colors = heightColors;
            }
        }
        // re play the animation (InstantiateMesh will colour the points with the new values)
        restart = true;
    }

    void playFrame(){
        // animation still playing
        if (play && indexIt < numPointGroups){
            InstantiateMesh(indexIt, groupPoints);
            indexIt++;
        }
        // animation finished
        else if(indexIt >= numPointGroups){
            indexIt = 0;
            play = false;
            restart = false;
        }

        // user want to replay the animation
        if (!play && Input.GetKeyDown("space"))
        {
            restart = true;
        }
        if (restart)
        {
            // destroy the old meshes and instantiate new ones
            foreach (Transform child in pointCloud.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
            play = true;
            restart = false;
            indexIt = 0;
        }
    }

	void loadScene(){
		// Check if the PointCloud was loaded previously
			loadPointCloud ();
	}	
    	
	void loadPointCloud(){
		// Check what file exists
		if (File.Exists (currentBaseDirectory + "/Assets/Models/" + dataPath + ".xyz")) {
			Debug.Log("STARTO COROUTINE: " + dataPath); 
			// Load XYZ
			StartCoroutine ("loadXYZ",  "/Models/" + dataPath + ".xyz");
		}
		else 
			Debug.Log ("File '" + currentBaseDirectory + "/Assets/Models/" + dataPath + "' could not be found"); 
		
	}
	
	// Load stored PointCloud
	void loadStoredMeshes(){

		Debug.Log ("Using previously loaded PointCloud: " + filename);

		GameObject pointGroup = Instantiate(Resources.Load ("PointCloudMeshes/" + filename)) as GameObject;

		loaded = true;
	}
	
	// Start Coroutine of reading the points from the XYZ file and creating the meshes
	IEnumerator loadXYZ(string dPath){

		// Read file
		numPoints = File.ReadAllLines (Application.dataPath + dPath).Length;

		StreamReader sr = new StreamReader (Application.dataPath + dPath);

		points = new Vector3[numPoints];
        colors = new Color[numPoints];
		defaultColors = new Color[numPoints];
        inputColors = new Color[numPoints];
        heightColors = new Color[numPoints];
        minValue = new Vector3();
        
        for (int i = 0; i < numPoints; i++) {
            string[] buffer = sr.ReadLine().Split(',');

            if (!invertYZ)
                points[i] = new Vector3(float.Parse(buffer[0], CultureInfo.InvariantCulture) * scale, float.Parse(buffer[1], CultureInfo.InvariantCulture) * scale, float.Parse(buffer[2], CultureInfo.InvariantCulture) * scale);
            else
                points[i] = new Vector3(float.Parse(buffer[0], CultureInfo.InvariantCulture) * scale, float.Parse(buffer[2], CultureInfo.InvariantCulture) * scale, float.Parse(buffer[1], CultureInfo.InvariantCulture) * scale);

            inputColors[i] = defaultPointColour;

            if (buffer.Length >= 5)
                defaultColors[i] = new Color(int.Parse(buffer[3], CultureInfo.InvariantCulture) / 255.0f, int.Parse(buffer[4], CultureInfo.InvariantCulture) / 255.0f, int.Parse(buffer[5], CultureInfo.InvariantCulture) / 255.0f);
            else
                defaultColors[i] = defaultPointColour;

            if (invertYZ)
                localDiff = float.Parse(buffer[1], CultureInfo.InvariantCulture) - minHeight;
            else
                localDiff = float.Parse(buffer[2], CultureInfo.InvariantCulture) - minHeight;
            heightColors[i] = colourGradient.Evaluate(localDiff / heightDiff);

            // Test enum for technique to colour points
            // Apply default point colour
            if (colourPointsBy == cpb.Default)
            {
                colors[i] = inputColors[i];
            }

            // Colour points by RGB values
            if (colourPointsBy == cpb.RGB)
            {
                colors[i] = defaultColors[i];
            }

            // TO DO - Automate calculation of minHeight and maxHeight
            // Colour points by Height
            else if (colourPointsBy == cpb.Height)
            {
                colors[i] = heightColors[i];
            }

            //TO DO - Automate calculation of minIntensity and maxIntensity
            // Colour points by intensity 
            else if (colourPointsBy == cpb.Intensity)
            {
                relativeDiff = float.Parse(buffer[6], CultureInfo.InvariantCulture) - minIntensity;
                colors[i] = colourGradient.Evaluate(relativeDiff / intensityDiff);
            }

            // Relocate points near the origin
            if (relocateToOrigin == true)
            {
                calculateMin(points[i]);
            }

            // Processing GUI
            progress = i *1.0f/(numPoints-1)*1.0f;
			if (i%Mathf.FloorToInt(numPoints/20) == 0)
            {
				guiText=i.ToString() + " out of " + numPoints.ToString() + " loaded";
				yield return null;
			}

        }

        // Instantiate Point Groups
        //numPointGroups = Mathf.CeilToInt(numPoints * 1.0f / limitPoints * 1.0f);
        groupPoints = Mathf.CeilToInt(numPoints * 1.0f / numPointGroups * 1.0f);

        //Debug.Log("Points group #: " + numPointGroups);
        pointCloud = new GameObject(filename);
        pointCloud.transform.parent = transform;

        //Store PointCloud
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(pointCloud, "Assets/Resources/PointCloudMeshes/" + filename + ".prefab");

		loaded = true;
	}
	
	void InstantiateMesh(int meshInd, int nPoints){
        // Create Mesh
        GameObject pointGroupTransform = new GameObject(filename + meshInd + "T");
        pointGroupTransform.transform.parent = pointCloud.transform;

        // create point group
        GameObject pointGroup = new GameObject (filename + meshInd);
		pointGroup.AddComponent<MeshFilter> ();
		pointGroup.AddComponent<MeshRenderer> ();
		pointGroup.GetComponent<Renderer>().material = matVertex;

		pointGroup.GetComponent<MeshFilter> ().mesh = CreateMesh (meshInd, groupPoints, groupPoints);
		pointGroup.transform.parent = pointGroupTransform.transform;
        pointGroup.transform.position = pointCloud.transform.position;
        pointGroup.transform.rotation = pointCloud.transform.rotation;
        pointGroup.transform.localScale = Vector3.Scale(pointGroup.transform.localScale, new Vector3(1, -1, 1));

        // Store Mesh
        UnityEditor.AssetDatabase.CreateAsset(pointGroup.GetComponent<MeshFilter> ().mesh, "Assets/Resources/PointCloudMeshes/" + filename + meshInd + ".asset");
		UnityEditor.AssetDatabase.SaveAssets ();
		UnityEditor.AssetDatabase.Refresh();
	}

	Mesh CreateMesh(int id, int nPoints, int limitPoints){
		
		Mesh mesh = new Mesh ();

        if(id*limitPoints + groupPoints > numPoints){
            nPoints = numPoints - id * limitPoints;
        }    
		
		Vector3[] myPoints = new Vector3[nPoints]; 
		int[] indecies = new int[nPoints];
		Color[] myColors = new Color[nPoints];

		for(int i=0;i<nPoints;++i) {
			myPoints[i] = points[id*limitPoints + i] - minValue;
			indecies[i] = i;
			myColors[i] = colors[id*limitPoints + i];
		}

		mesh.vertices = myPoints;
		mesh.colors = myColors;
		mesh.SetIndices(indecies, MeshTopology.Points,0);
		mesh.uv = new Vector2[nPoints];
		mesh.normals = new Vector3[nPoints];

		return mesh;
	}

	void calculateMin(Vector3 point){
		if (minValue.magnitude == 0)
			minValue = point;


		if (point.x < minValue.x)
			minValue.x = point.x;
		if (point.y < minValue.y)
			minValue.y = point.y;
		if (point.z < minValue.z)
			minValue.z = point.z;
	}

	void createFolders(){
		if(!Directory.Exists (Application.dataPath + "/Resources/"))
			UnityEditor.AssetDatabase.CreateFolder ("Assets", "Resources");

		if (!Directory.Exists (Application.dataPath + "/Resources/PointCloudMeshes/"))
			UnityEditor.AssetDatabase.CreateFolder ("Assets/Resources", "PointCloudMeshes");
	}

	/*void OnGUI(){


		if (!loaded){
			GUI.BeginGroup (new Rect(Screen.width/2-100, Screen.height/2, 400.0f, 20));
			GUI.Box (new Rect (0, 0, 200.0f, 20.0f), guiText);
			GUI.Box (new Rect (0, 0, progress*200.0f, 20), "");
			GUI.EndGroup ();
		}
	}*/

}
