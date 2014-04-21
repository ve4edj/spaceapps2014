#pragma strict
var windowRect : Rect = Rect (20, 20, 160, 50);
var sec : float;
var timeMultiplier : float = 1;
var showDropMenu : boolean = false;
var cameraMode : boolean = false;
var showLines : boolean = true;
var scrollPosition : Vector2;
var mainMenu : boolean = true;

var username : String = "username";
var password : String = "password";
var block : String =  "*";
var ch : char = block[0];
function Start () {
this.DontDestroyOnLoad(this);
}

function FixedUpdate () {
if (!mainMenu)
{
	sec += Time.deltaTime * timeMultiplier;
}
}

function OnGUI () {
	if (!mainMenu)
	{
		windowRect = GUILayout.Window (0, windowRect, DoMyWindow, "Time");
	}
	if (mainMenu)
	{
		windowRect = GUILayout.Window (0, windowRect, menuWindow, "Solar System\nExplorer");
	}
	this.GetComponent(MouseOrbit).enabled = !mainMenu;
}

function DoMyWindow (windowID : int) {
GUILayout.BeginHorizontal();
GUILayout.Label("" + sec);
GUILayout.EndHorizontal();
GUILayout.Label("Acceleration");
GUILayout.BeginHorizontal();
if (GUILayout.Button("<<") && timeMultiplier >= 10)
{
	timeMultiplier /= 10;
}
if (GUILayout.Button(">>") && timeMultiplier <= 1000000)
{
	timeMultiplier *= 10;
}
GUILayout.EndHorizontal();
GUILayout.Label("" + timeMultiplier + "x");
if (GUILayout.Button("Object: " + camera.main.GetComponent(MouseOrbit).target.name))
{
	if (!showDropMenu)
	{
		showDropMenu = true;
	}
	else if (showDropMenu)
	{
		showDropMenu = false;
	}
}
if (showDropMenu)
{
	var objects : GameObject[] = GameObject.FindGameObjectsWithTag("Planet");
	scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(100),GUILayout.Height(100));
	for (var i : int = 0; i < objects.Length; i++)
	{
		if (GUILayout.Button(objects[i].name))
		{
			Camera.main.GetComponent(MouseOrbit).SendMessage("setTarget",objects[i].transform);
			showDropMenu = false;
		}
	}
	
	GUILayout.EndScrollView();
}
if (GUILayout.Button("Camera Mode"))
{
	if (cameraMode)
	{
		cameraMode = false;
	}
	else if (!cameraMode)
	{
		cameraMode = true;
	}
}
showLines = GUILayout.Toggle(showLines, "Show Orbits");
}

function menuWindow(windowID : int)
{
GUILayout.Label("");
if (GUILayout.Button("Start"))
{
	mainMenu = false;
	//this.GetComponent(mouseOrbit).target = gameObject.
	Application.LoadLevel("solarsystem");
}
if (GUILayout.Button("Quit"))
{
	Application.Quit();
}
GUILayout.Label("Username");
username = GUILayout.TextArea(username);
GUILayout.Label("Password");
password = GUILayout.PasswordField(password, ch);
}