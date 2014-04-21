#pragma strict
var rotateSpeed : float;
function Start () {

}

function Update () {
this.transform.Rotate(Vector3.up*rotateSpeed*Time.deltaTime);
}