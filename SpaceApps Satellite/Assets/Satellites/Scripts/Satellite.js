#pragma strict
//var positions = [Vector3(0,1,0),Vector3(1,0,0),Vector3(0,-1,0),Vector3(-1,0,0)];
//var velocities = [0.1,0.3,0.7,0.1];
var positions : Vector3[];
var velocities : float[];
var nextPositions = 0;
var parent : Transform;
var TimeMultiplier : int = 1;
var DisplayColor : Color = Color.blue;
var DisplayTexture : Texture2D;
var DisplayTiling : int = 50;
private var LR : LineRenderer;

function Start () {
this.transform.position = parent.position - positions[nextPositions];
SetupDraw();
}

function Update () {
updateDraw();
TimeMultiplier = Camera.main.GetComponent(MainScript).timeMultiplier;
this.transform.position = Vector3.MoveTowards(this.transform.position, parent.position - positions[nextPositions], Time.deltaTime*velocities[nextPositions]*TimeMultiplier);
if (Vector3.Distance(this.transform.position, parent.position - positions[nextPositions]) < 0.01 && nextPositions < positions.Length-1)
{
	nextPositions++;
}
else if (Vector3.Distance(this.transform.position, parent.position - positions[nextPositions]) < 0.01 && nextPositions >= positions.Length-1)
{
	nextPositions = 0;
}
this.GetComponent(TextMesh).transform.LookAt(Camera.main.transform);
this.GetComponent(TextMesh).transform.rotation.eulerAngles.x -= 180;
this.GetComponent(TextMesh).text = this.name;
}

function SetupDraw(){
var Orbit : GameObject = new GameObject("Orbit_Path");
Orbit.transform.parent = GameObject.Find("Orbits").transform;
Orbit.transform.position = parent.position;
Orbit.AddComponent(LineRenderer);
LR = Orbit.GetComponent(LineRenderer);
LR.SetWidth(0.02,0.02);
LR.material.shader = Shader.Find("Particles/Additive");
LR.material.SetColor ("_TintColor", DisplayColor);
if(DisplayTexture != null){
		LR.material.mainTexture = DisplayTexture;
		LR.material.mainTextureScale.x = DisplayTiling;
	}
LR.SetVertexCount(positions.Length+1);
for (var i : int = 0; i < (positions.Length); i++){
	LR.SetPosition(i, parent.position - positions[i]);
}
LR.SetPosition(positions.Length, parent.position - positions[0]);
}

function updateDraw()
{
for (var i : int = 0; i < (positions.Length); i++){
LR.SetPosition(i, parent.position - positions[i]);
}
LR.SetPosition(positions.Length, parent.position - positions[0]);
LR.enabled = Camera.main.GetComponent(MainScript).showLines;
}
