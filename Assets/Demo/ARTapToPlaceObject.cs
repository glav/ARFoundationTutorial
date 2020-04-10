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
    public GameObject objectToPlace;
    public Light mainLight;
    public GameObject debugObject;
    [SerializeField]
    [Tooltip("The ARCameraManager which will produce frame events containing light estimation information.")]
    ARCameraManager arCameraManager;

    private ARSessionOrigin arOrigin;
    private ARPlaneManager arPlaneManager;
    private ARRaycastManager arRayCastMgr;

    private Pose placementPose;
    private bool placementPoseIsValid = false;
    private TrackableId currentTrackedItem;
    private Color oldColor = Color.white;

    void FrameChanged (ARCameraFrameEventArgs args)
    {
        if (args.lightEstimation.averageBrightness.HasValue)
        {
            var brightness = args.lightEstimation.averageBrightness.Value;
            mainLight.intensity = brightness;
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        arOrigin = FindObjectOfType<ARSessionOrigin>();
        arPlaneManager = arOrigin.GetComponent<ARPlaneManager>();
        arRayCastMgr = arOrigin.GetComponent<ARRaycastManager>();
        arCameraManager = arOrigin.camera.GetComponent<ARCameraManager>();

        if (arCameraManager != null)
            arCameraManager.frameReceived -= FrameChanged;

        if (arCameraManager != null & enabled)
            arCameraManager.frameReceived += FrameChanged;


    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementPoseIndicator();

        if (placementPoseIsValid && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            ChangeDebugColor(Color.green);
            PlaceObject(placementPose.position, placementPose.rotation);
            return;
        }
        else
        {
            if (Application.isEditor && Input.GetMouseButtonDown(0))
            {
                var camera = arOrigin.camera;
                var screenCenter = camera.transform.position;
                var cameraFwd = camera.transform.forward;
                var position = screenCenter + new Vector3(0, 0,0.5f);
                var cameraBearing = new Vector3(cameraFwd.x, 0, cameraFwd.z).normalized;
                var rotation = Quaternion.LookRotation(cameraBearing);

                ChangeDebugColor(Color.magenta);
                PlaceObject(position, rotation);
                return;
            }
        }

        if (placementPoseIsValid)
        {
            ChangeDebugColor(Color.blue);
            return;
        }
        ChangeDebugColor(Color.red);
    }

    void ChangeDebugColor(Color newColor)
    {
        if (debugObject.activeSelf)
        {
            debugObject.GetComponent<Renderer>().material.SetColor("_Color", newColor);
        }
    }

    private void PlaceObject(Vector3 position, Quaternion rotation)
    {
        Instantiate(objectToPlace, position, rotation);
    }

    private void UpdatePlacementPoseIndicator()
    {
        if (Application.isEditor) return;

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
        }
        else
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
