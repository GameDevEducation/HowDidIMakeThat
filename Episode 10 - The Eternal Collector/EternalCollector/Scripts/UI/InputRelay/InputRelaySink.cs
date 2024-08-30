using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InputRelaySink : MonoBehaviour
{
    [SerializeField] RectTransform CanvasTransform;

    GraphicRaycaster Raycaster;

    List<GameObject> DragTargets = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        Raycaster = GetComponent<GraphicRaycaster>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    List<GameObject> TemporaryIgnoreList = new List<GameObject>();

    public void OnCursorInput(Vector2 normalisedPosition)
    {
        // calculate the position in canvas space
        Vector3 mousePosition = new Vector3(CanvasTransform.sizeDelta.x * normalisedPosition.x,
                                            CanvasTransform.sizeDelta.y * normalisedPosition.y,
                                            0f);

        // construct our pointer event
        PointerEventData mouseEvent = new PointerEventData(EventSystem.current);
        mouseEvent.position = mousePosition;

        // perform a raycast using the graphics raycaster
        List<RaycastResult> results = new List<RaycastResult>();
        Raycaster.Raycast(mouseEvent, results);
        
        bool sendMouseDown = Mouse.current.leftButton.wasPressedThisFrame;
        bool sendMouseUp = Mouse.current.leftButton.wasReleasedThisFrame;
        bool isMouseDown = Mouse.current.leftButton.isPressed;

        // send through end drag events as needed
        if (sendMouseUp)
        {
            foreach (var target in DragTargets)
            {
                if (ExecuteEvents.Execute(target, mouseEvent, ExecuteEvents.endDragHandler))
                    break;
            }
            DragTargets.Clear();
        }

        // process the raycast results
        foreach (var result in results)
        {
            var workingGO = result.gameObject;

            // setup the new event data
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = mousePosition;
            eventData.pointerCurrentRaycast = eventData.pointerPressRaycast = result;

            // is the mouse down?
            if (isMouseDown)
                eventData.button = PointerEventData.InputButton.Left;

            // potentially new drag targets?
            if (sendMouseDown)
            {
                ExecuteEvents.Execute(workingGO, eventData, ExecuteEvents.pointerDownHandler);

                if (ExecuteEvents.CanHandleEvent<IDragHandler>(workingGO))
                {
                    ExecuteEvents.Execute(workingGO, eventData, ExecuteEvents.initializePotentialDrag);
                    ExecuteEvents.Execute(workingGO, eventData, ExecuteEvents.beginDragHandler);
                    DragTargets.Add(workingGO);
                }
            } 
            
            // need to update drag target
            if (DragTargets.Contains(workingGO))
            {
                eventData.dragging = true;
                ExecuteEvents.Execute(workingGO, eventData, ExecuteEvents.dragHandler);
            }

            // send a mouse up or click event?
            if (sendMouseUp)
            {
                bool didRun = ExecuteEvents.Execute(workingGO, eventData, ExecuteEvents.pointerUpHandler);
                didRun |= ExecuteEvents.Execute(workingGO, eventData, ExecuteEvents.pointerClickHandler);

                if (didRun)
                    break;
            }
        }
    }
}