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

public class CarPointCloudManager : MonoBehaviour {

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
    private bool recolor = false;

    // Point cloud properties
    private GameObject pointCloud;
    public GameObject showcase;
    public GameObject canvas;
    public Camera myCam;
    private GameObject meshText;

    public float scale = 1;

	private Vector3[] points;
	private Vector3[] normals;
	private Vector3 minValue;
    private float[] pressure;
    private Color[] colors;
    private Color[] tempColors;

    private float avgPressure = 0;
    private float percPoints = 100;
    int currIndex = -1;

    public int numPoints = 486030;
    public int numPointGroups;
    //private int limitPoints = 603000;
    private int groupPoints = 65535;

    // Point height properties
    public float minHeight;
    public float maxHeight;
    public float minPressure;
    public float maxPressure;
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

        //Calculate height difference for the visualising height gradient
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

    numPointGroups = (int) Mathf.Ceil(numPoints * 1.0f / groupPoints * 1.0f);

	loadPointCloud();
    //loadStoredMeshes();

    transform.position = new Vector3(2.57f, -0.473f, 3.56f);
    
    }

    void Update()
    {
        if (loaded)
        {
            // display the data
            displayAvg();

            // ray casting
            GetMouseInfo();
        }
    }

    void GetMouseInfo()
    {
        for (int id = 0; id < numPointGroups; id++)
        {
            // retrieve the ray
            Ray ray = myCam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // cast the ray
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                // check for hits
                if (hit.collider.transform.name == "mesh_simple.xyz" + id.ToString())
                {
                    // if it hits a different component, highlight the new one
                    if (id != currIndex)
                    {
                        Destroy(meshText);
                        highlightGroup(id, true);
                        recolor = true;
                        currIndex = id;
                    }
                }
            }
            // no components are hovered
            else if(recolor)
            {
                highlightGroup(id, false);
                recolor = false;
                currIndex = -1;
            }
        }
    }

    // highlight the corresponding mesh
    void highlightGroup(int meshInd, bool highlight)
    {
        for (int id = 0; id < numPointGroups; id++)
        {
            // find the point group
            GameObject currChild = GameObject.Find("/GameObject (1)/mesh_simple.xyz/mesh_simple.xyz" + id.ToString());
            Mesh mesh = currChild.GetComponent<MeshFilter>().mesh;

            // compute the number of points
            int selectPoints = groupPoints;
            if (id * groupPoints + groupPoints > numPoints)
            {
                selectPoints = numPoints - id * groupPoints;
            }

            // iterate all the points of the group
            float sumPress = 0;
            Color[] selectColors = new Color[selectPoints];
            if (highlight)
            {
                // re-colour just the group that are not selected
                for (int i = 0; i < selectPoints; i++)
                {
                    if (id != meshInd)
                    {
                        selectColors[i] = new Color(0.75f, 0.75f, 0.75f, 0.1f);
                    }
                    else
                    {
                        selectColors[i] = colors[id * groupPoints + i];
                        sumPress += pressure[id * groupPoints + i];
                    }
                }
                // show the average
                if(id == meshInd)
                {
                    meshText = new GameObject("Mesh Text");
                    meshText.transform.SetParent(canvas.transform.parent.gameObject.transform);

                    float tempAvg = sumPress / selectPoints;
                    meshText.AddComponent<Text>().text = "Avg Pressure: " + tempAvg.ToString("0.###");
                    meshText.GetComponent<Text>().font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
                    meshText.GetComponent<Text>().fontSize = 12;
                    meshText.GetComponent<Text>().color = Color.black;

                    meshText.GetComponent<Text>().transform.position = Input.mousePosition + new Vector3(0, 50, 0);
                    //meshText.GetComponent<Text>().transform.position = currChild.GetComponent<BoxCollider>().transform.position;
                    meshText.GetComponent<RectTransform>().sizeDelta += new Vector2(100, -80);
                }
            }
            // retrieve the original colours
            else if(!highlight)
            {
                for (int i = 0; i < selectPoints; i++)
                {
                    //selectColors[i] = colors[id * groupPoints + i];
                    selectColors[i] = tempColors[id * groupPoints + i];
                }
                Destroy(meshText);
            }
            mesh.colors = selectColors;
        }
    }

    // display the data
    void displayAvg()
    {
        canvas.gameObject.GetComponent<Text>().text = "Avg Pressure: " + avgPressure.ToString("0.###") + "\n" + "Ratio: " + percPoints.ToString("0.#") + " %";
    }

    // select range of points
    public void selectPoints(float minPressure)
    {
        // iterate all the group points
        float tempNumPoints = 0;
        float tempAvgPress = 0;
        for (int id = 0; id < numPointGroups; id++)
        {
            // retrieve the mesh
            GameObject currChild = GameObject.Find("/GameObject (1)/mesh_simple.xyz/mesh_simple.xyz" + id.ToString());
            Mesh mesh = currChild.GetComponent<MeshFilter>().mesh;

            // compute the number of points
            int selectPoints = groupPoints;
            if (id * groupPoints + groupPoints > numPoints)
            {
                selectPoints = numPoints - id * groupPoints;
            }

            Color[] selectColors = new Color[selectPoints];
            for (int i = 0; i < selectPoints; ++i)
            {
                // colour in gray all the points < that min pressure
                if (minPressure > -2 && pressure[id * groupPoints + i] < minPressure)
                {
                    selectColors[i] = new Color(0.75f, 0.75f, 0.75f, 0.1f);
                    tempColors[id * groupPoints + i] = selectColors[i];
                }
                else
                {
                    selectColors[i] = colors[id * groupPoints + i];
                    tempColors[id * groupPoints + i] = selectColors[i];

                    tempNumPoints ++;
                    tempAvgPress += pressure[id * groupPoints + i];
                }
            }
            mesh.colors = selectColors;
        }

        // compute average and percentage of active points
        avgPressure = tempAvgPress / tempNumPoints;
        percPoints = (tempNumPoints / numPoints) * 100;
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

		Debug.Log ("Using previously loaded PointCloud: " + filename + ".prefab");

		GameObject pointCloud = Instantiate(Resources.Load ("PointCloudMeshes/" + filename + ".prefab")) as GameObject;

		loaded = true;
	}
	
	// Start Coroutine of reading the points from the XYZ file and creating the meshes
	IEnumerator loadXYZ(string dPath){

		// Read file
		numPoints = File.ReadAllLines (Application.dataPath + dPath).Length;

		StreamReader sr = new StreamReader (Application.dataPath + dPath);

		points = new Vector3[numPoints];
		normals = new Vector3[numPoints];
		minValue = new Vector3();
        pressure = new float[numPoints];
        colors = new Color[numPoints];
        tempColors = new Color[numPoints];
		
		for (int i = 0; i< numPoints; i++){
			string[] buffer = sr.ReadLine ().Split(',');

			points[i] = new Vector3 (float.Parse (buffer[0], CultureInfo.InvariantCulture) *scale, float.Parse (buffer[2], CultureInfo.InvariantCulture) *scale,float.Parse (buffer[1], CultureInfo.InvariantCulture) *scale);
            normals[i] = new Vector3(float.Parse(buffer[3], CultureInfo.InvariantCulture), float.Parse(buffer[4], CultureInfo.InvariantCulture), float.Parse(buffer[5], CultureInfo.InvariantCulture));
            pressure[i] = float.Parse(buffer[6], CultureInfo.InvariantCulture);

            avgPressure += pressure[i];

            // Processing GUI
            progress = i *1.0f/(numPoints-1)*1.0f;
			if (i%Mathf.FloorToInt(numPoints/20) == 0)
            {
				guiText=i.ToString() + " out of " + numPoints.ToString() + " loaded";
				yield return null;
			}

		}

        avgPressure = avgPressure / numPoints;

        // create pointcloud
        pointCloud = new GameObject(filename);
        // attach it to the game object
        pointCloud.transform.parent = transform;
        //pointCloud.transform.position = showcase.transform.position;
        pointCloud.transform.position -= new Vector3(-2.57f, 0.473f, -3.56f);

        for (int i = 0; i < numPointGroups; i++)
        {
            InstantiateMesh(i, groupPoints);
        }

        loaded = true;
	}
	
	void InstantiateMesh(int meshInd, int nPoints)
    {
        // create mesh
        GameObject pointGroup = new GameObject (filename + meshInd);
		pointGroup.AddComponent<MeshFilter> ();
		pointGroup.AddComponent<MeshRenderer> ();
		pointGroup.GetComponent<Renderer>().material = matVertex;

		pointGroup.GetComponent<MeshFilter> ().mesh = CreateMesh (meshInd, groupPoints, groupPoints);
        // attach to point cloud
		pointGroup.transform.parent = pointCloud.transform;
        pointGroup.transform.position = pointCloud.transform.position;
        pointGroup.transform.rotation = pointCloud.transform.rotation;
        // add box collider
        BoxCollider boxCollider = pointGroup.AddComponent<BoxCollider>();

        //Store PointCloud
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(pointGroup, "Assets/Resources/PointCloudMeshes/" + filename + ".prefab");

        // Store Mesh
        UnityEditor.AssetDatabase.CreateAsset(pointGroup.GetComponent<MeshFilter> ().mesh, "Assets/Resources/PointCloudMeshes/" + filename + ".asset");
		UnityEditor.AssetDatabase.SaveAssets ();
		UnityEditor.AssetDatabase.Refresh();
	}

	Mesh CreateMesh(int id, int nPoints, int limitPoints){
		
		Mesh mesh = new Mesh ();

        if (id * limitPoints + groupPoints > numPoints)
        {
            nPoints = numPoints - id * limitPoints;
        }

        Vector3[] myPoints = new Vector3[nPoints]; 
		int[] indecies = new int[nPoints];
        Vector3[] myNormals = new Vector3[nPoints];
        Color[] myColors = new Color[nPoints];

        for (int i=0;i<nPoints; ++i) {
			myPoints[i] = points[id * limitPoints + i];
			indecies[i] = i;
            myNormals[i] = normals[id * limitPoints + i];
            float grad = Mathf.InverseLerp(minPressure, maxPressure, pressure[id * limitPoints + i]);
            myColors[i] = colourGradient.Evaluate(grad);
            colors[id * limitPoints + i] = colourGradient.Evaluate(grad);
            tempColors[id * limitPoints + i] = colourGradient.Evaluate(grad);
        }

		mesh.vertices = myPoints;
        mesh.colors = myColors;
        mesh.SetIndices(indecies, MeshTopology.Points,0);
		mesh.uv = new Vector2[nPoints];
        mesh.normals = myNormals;

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

	void OnGUI(){


		if (!loaded){
			GUI.BeginGroup (new Rect(Screen.width/2-100, Screen.height/2, 400.0f, 20));
			GUI.Box (new Rect (0, 0, 200.0f, 20.0f), guiText);
			GUI.Box (new Rect (0, 0, progress*200.0f, 20), "");
			GUI.EndGroup ();
		}
	}

}
