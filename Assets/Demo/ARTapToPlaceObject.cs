using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Linq;

[RequireComponent(typeof(ARSessionOrigin))]
[RequireComponent(typeof(ARRaycastManager))]

public class ARTapToPlaceObject : MonoBehaviour
{
    public GameObject placementIndicator;

    private ARSessionOrigin arOrigin;
    private ARPlaneManager arPlaneManager;
    private ARRaycastManager arRayCastMgr;
    private Pose placementPose;
    private bool placementPoseIsValid = false;
    private TrackableId currentTrackedItem;


    // Start is called before the first frame update
    void Start()
    {
        arOrigin = FindObjectOfType<ARSessionOrigin>();
        arPlaneManager = arOrigin.GetComponent<ARPlaneManager>();
        arRayCastMgr = arOrigin.GetComponent<ARRaycastManager>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementPoseIndicator();
    }

    private void UpdatePlacementPoseIndicator()
    {
        // this routine is done as sometimes 'trackable' can be left behind as it detect weird surfaces 
        // and this leaves placement indicators everywhere. 
        IEnumerator ClearOldTrackables()
        {
            foreach (var tracked in arPlaneManager.trackables)
            {
                if (tracked.trackableId != currentTrackedItem)
                {
                    tracked.gameObject.SetActive(false);
                }
                yield return null;
            }

        }

        StartCoroutine(ClearOldTrackables());

        if (placementPoseIsValid)
        {
            placementIndicator.SetActive(true);
            placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
        } else
        {
            placementIndicator.SetActive(false);
        }
    }

    private void UpdatePlacementPose()
    {
        var camera = arOrigin.camera;

        var screenCenter = camera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));

        var hitsList = new List<ARRaycastHit>();

        arRayCastMgr.Raycast(screenCenter, hitsList, TrackableType.Planes);
        placementPoseIsValid = hitsList.Count > 0;
        if (placementPoseIsValid)
        {
            placementPose = hitsList[0].pose;
            currentTrackedItem = hitsList[0].trackableId;

            var cameraFwd = camera.transform.forward;
            var cameraBearing = new Vector3(cameraFwd.x, 0, cameraFwd.z).normalized;
            placementPose.rotation = Quaternion.LookRotation(cameraBearing);
        }
    }
}
