using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Takes care of the generation and deletion of the signs
/// </summary>
public class SignManager : Singleton<SignManager>
{

    [SerializeField]
    private GameObject signPrefab;

    List<Sign> signs;

    /// <summary>
    /// signals that there is no more signs in the list
    /// </summary>
    public UnityEvent LastSignRemoved;
    /// <summary>
    /// signlas that there the first sign was added to the list
    /// </summary>
    public UnityEvent FirstSignAdded;

    [SerializeField]
    UISignZoom uiSignZoom;

    private void Start()
    {
        signs = new List<Sign>();
    }

    /// <summary>
    /// Creates sign based on the object hit and the position where it was hit
    /// </summary>
    /// <param name="hit">the RaycastHit created by the raycast</param>
    public void CreateSign( RaycastHit hit) {
        GameObject obj = hit.transform.gameObject;
        //Extract Pipe Info
        Pipe pipe = obj.GetComponent<Pipe>();
        if (pipe == null) {
            return;
        }

        //Find the real point of intersection with the pipe
        int section; double parameter;
        (section, parameter) = FindThePoint(pipe, hit.point);
        Vector3D positionWGS = GetSignPositionWGS(pipe, section, parameter);
        Vector3D positionUTM = GetSignPositionPlanar(pipe, section, parameter);

        //Create Sign
        GameObject gameObject = Instantiate(signPrefab);
        gameObject.transform.SetParent(pipe.transform);
        gameObject.transform.position = pipe.transform.position;

        gameObject.transform.localPosition = positionUTM.ToFloat();

        gameObject.GetComponentInChildren<OpenUI>().uiSignZoom = uiSignZoom;

        Sign sign = gameObject.GetComponent<Sign>();
        sign.SetSign(pipe.dbfData, positionWGS, uiSignZoom);
        signs.Add(sign);

        if (signs.Count == 1) {
            FirstSignAdded.Invoke();
        }

    }

    /// <summary>
    /// Finds the closest point on the line describing pipe and finds which segment of the pipe it was
    /// </summary>
    /// <param name="pipe">The pipe which was hit</param>
    /// <param name="point">The point of the hit</param>
    /// <returns>The section which is closest to the point and the parameter from the first point of the section</returns>
    private (int section, double parameter) FindThePoint(Pipe pipe, Vector3 point) {
        int section = 0;
        double minDistance = double.MaxValue;
        double parameter = 0;

        //get the hit point into the local coordinates of the pipe
        Matrix4x4 inverseMatrix = pipe.transform.worldToLocalMatrix;
        Vector3 local = inverseMatrix.MultiplyPoint(point);

        Vector3D pointD = new Vector3D(local.x, local.y, local.z);
        
        
        for (int i = 0; i < pipe.pointsInPlanar.Count-1; i++) {
            //find the point on the segment closest to the hitpoint
            Vector3D realFirstPointPos =   pipe.pointsInPlanar[i];
            Vector3D realSecondPointPos = pipe.pointsInPlanar[i + 1];

            Vector3D lineDirection = realSecondPointPos - realFirstPointPos;
            Vector3D otherDirection = pointD - realFirstPointPos;

            double t = lineDirection.DotProduct(otherDirection) / lineDirection.DotProduct(lineDirection);

            t = Math.Clamp(t, 0, 1);

            Vector3D closestPoint = realFirstPointPos + t * lineDirection;

            //check if the point was not closer to the previous segments
            double distance = closestPoint.Distance(pointD);
            if (distance < minDistance) {
                minDistance = distance;
                section = i;
                parameter = t;
            }
        }

        return (section, parameter);  
    }

    /// <summary>
    /// Retrieves the WGS coordinates by interpolation
    /// </summary>
    /// <param name="pipe">the pipe which we are using</param>
    /// <param name="section">the section of the pipe which we are using</param>
    /// <param name="parameter">the interpolation parameter</param>
    /// <returns>the WGS coordinates of the new point</returns>
    private Vector3D GetSignPositionWGS(Pipe pipe, int section, double parameter) {
        Vector3D direction = pipe.pointsInWGS[section + 1 ] - pipe.pointsInWGS[section];
        Vector3D position = pipe.pointsInWGS[section] + parameter * direction;
        return position;
    }
    /// <summary>
    /// Retireves the planar coordinates by interpolation
    /// </summary>
    /// <param name="pipe">the pipe which we are using</param>
    /// <param name="section">the section of the pipe which we are using</param>
    /// <param name="parameter">the interpolation parameter</param>
    /// <returns>the planar coordinate of the new point</returns>
    private Vector3D GetSignPositionPlanar(Pipe pipe, int section, double parameter)
    {
        Vector3D direction = pipe.pointsInPlanar[section + 1] - pipe.pointsInPlanar[section];
        Vector3D position = pipe.pointsInPlanar[section] + parameter * direction;
        return position;
    }
    /// <summary>
    /// Destroys all of the signs in the list
    /// </summary>
    public void DestroyAllSigns() {
        for (int i = signs.Count - 1; i >= 0; i--) {
            StartCoroutine(signs[i].DestroyAnimation());
        }
        signs.Clear();
        LastSignRemoved.Invoke();
    }
    /// <summary>
    /// Finds and remove a sign from the list
    /// </summary>
    /// <param name="sign">The sign to be removed</param>
    public void RemoveSign(Sign sign) {
        for (int i = 0; i < signs.Count; i++) {
            if (signs[i].gameObject == sign.gameObject) {                
                StartCoroutine(signs[i].DestroyAnimation());
                signs.RemoveAt(i);
                if (signs.Count == 0) {
                    LastSignRemoved.Invoke();
                }
                return;
            }
        }
    }

}
