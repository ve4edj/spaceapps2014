#pragma strict
var windowRect : Rect = Rect (20, 20, 160, 50);
var username : String = "username";
var password : String = "password";
var block : String =  "*";
var ch : char = block[0];
function Start () {

}

function Update () {

}

function OnGUI()
{
	windowRect = GUILayout.Window (0, windowRect, DoMyWindow, "Solar System\nExplorer");
}

function DoMyWindow (windowID : int) {
GUILayout.Label("");
if (GUILayout.Button("Start"))
{
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