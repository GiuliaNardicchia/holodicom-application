using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using PointerHandler = System.Action<Microsoft.MixedReality.Toolkit.Input.MixedRealityPointerEventData>;

public class SliceSlider : MonoBehaviour, IMixedRealityPointerHandler
{

    #region Public Nested Classes
    public class EventData
    {
        public int NewIndex { get; }

        public int NumberOfSlices { get; }

        public EventData(int newIndex, int slices) 
            => (NewIndex, NumberOfSlices) = (newIndex, slices);
    }
    #endregion

    #region Private Nested Classes
    private class SliderInteractionData
    {
        public Vector3 PointerStartingPosition { get; }

        public IMixedRealityPointer Pointer { get; }

        public int StartingIndex { get; }

        public SliderInteractionData(IMixedRealityPointer pointer, int index)
            => (PointerStartingPosition, Pointer, StartingIndex) = (pointer.Position, pointer, index);

    }

    private class PointerHandlerState
    {
        public PointerHandler OnClicked { get; }
        public PointerHandler OnDown { get; }
        public PointerHandler OnUp { get; }
        public PointerHandler OnDragged { get; }

        public PointerHandlerState(PointerHandler onClicked,
            PointerHandler onDown,
            PointerHandler onUp,
            PointerHandler onDragged
            ) => (OnClicked, OnDown, OnUp, OnDragged) = (onClicked, onDown, onUp, onDragged);

        private PointerHandlerState() : this(x => { }, x => { }, x => { }, x => { }) { }

        public static PointerHandlerState Uninitialized { get => new PointerHandlerState(); }

    }

    #endregion

    #region Public Properties and Serialized fields

    [SerializeField]
    private SliderAxis sliderAxis = SliderAxis.ZAxis;

    [SerializeField]
    private GameObject viewingPlane;
    public GameObject ViewingPlane
    {
        get => viewingPlane;
        set => viewingPlane = value;
    }

    #endregion

    #region Private Properties

    private SliderInteractionData ActiveInteraction { get; set; }

    private Vector3 AxisDirection
    {
        get => sliderAxis switch
        {
            SliderAxis.XAxis => Vector3.right,
            SliderAxis.YAxis => Vector3.up,
            SliderAxis.ZAxis => Vector3.forward,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private float SliderStartDistance { get; set; }

    private float SliderEndDistance { get; set; }

    private Vector3 SliderStartPosition
    {
        get => transform.TransformPoint(AxisDirection * SliderStartDistance);
    }

    private Vector3 SliderEndPosition
    {
        get => transform.TransformPoint(AxisDirection * SliderEndDistance);
    }

    private Vector3 SliderTrackDirection
    {
        get => SliderEndPosition - SliderStartPosition;
    }

    private float AbsoluteStepSizeNormalized
    {
        get => 1.0f / SliderDivisions;
    }

    private int SliderDivisions
    {
        get => sliceDepth.Length - 1;
    }

    private int? currentIndex = null;
    private int CurrentIndex
    {
        get => currentIndex ?? -1;
        set
        {
            if (currentIndex == value)
            {
                return;
            }
            currentIndex = value;
            UpdatePosition();
            OnIndexChanged.Invoke(new EventData(value, SliderDivisions + 1));
        }
    }


    private float[] sliceDepth;
    public void UseImages(IEnumerable<float> stepPositions, UnityAction<EventData> onIndexUpdated)
    {
        sliceDepth = stepPositions.Select(x => x - stepPositions.Min()).ToArray();
        var modelDepth = sliceDepth.Max() - sliceDepth.Min();
        SliderEndDistance = modelDepth / 2;
        SliderStartDistance = -modelDepth / 2;
        HandlerState = new PointerHandlerState(x => { }, PointerDown, PointerUp, PointerDragged);
        OnIndexChanged.AddListener(onIndexUpdated);
        CurrentIndex = 0;
    }

    private PointerHandlerState handlerState = PointerHandlerState.Uninitialized;
    private PointerHandlerState HandlerState
    {
        get => handlerState;
        set => handlerState = value;
    }

    #endregion

    #region Private Methods

    private void UpdatePosition()
    {
        viewingPlane.transform.position = SliderStartPosition + SliderTrackDirection * sliceDepth[CurrentIndex] / sliceDepth.Max();
    }

    private void PointerDown(MixedRealityPointerEventData eventData)
    {
        if (eventData.Pointer == null || eventData.used)
        {
            return;
        }
        ActiveInteraction = new SliderInteractionData(eventData.Pointer, CurrentIndex);
        OnInteractionStarted.Invoke(new EventData(CurrentIndex, SliderDivisions + 1));
        eventData.Use();
    }

    private void PointerDragged(MixedRealityPointerEventData eventData)
    {
        if (eventData.Pointer != ActiveInteraction?.Pointer || eventData.used)
        {
            return;
        }
        var distanceVector = ActiveInteraction.Pointer.Position - ActiveInteraction.PointerStartingPosition;
        var distanceAlongAxis = Vector3.Dot(SliderTrackDirection.normalized, distanceVector);
        var axisDistanceNormalized = distanceAlongAxis / SliderTrackDirection.magnitude;
        var numberOfStepsPassed = Mathf.Floor(Mathf.Abs(axisDistanceNormalized) / AbsoluteStepSizeNormalized);
        CurrentIndex = (int)Mathf.Clamp(
            ActiveInteraction.StartingIndex + Mathf.Sign(axisDistanceNormalized) * numberOfStepsPassed,
            0,
            SliderDivisions
        );
        eventData.Use();
    }

    private void PointerUp(MixedRealityPointerEventData eventData)
    {
        if (eventData.Pointer != ActiveInteraction?.Pointer || eventData.used)
        {
            return;
        }
        ActiveInteraction = null;
        OnInteractionEnded.Invoke(new EventData(CurrentIndex, SliderDivisions + 1));
        eventData.Use();
    }

    #endregion

    #region IMixedRealityPointerHandler

    public void OnPointerClicked(MixedRealityPointerEventData eventData) => HandlerState.OnClicked(eventData);

    public void OnPointerDown(MixedRealityPointerEventData eventData) => HandlerState.OnDown(eventData);

    public void OnPointerDragged(MixedRealityPointerEventData eventData) => HandlerState.OnDragged(eventData);

    public void OnPointerUp(MixedRealityPointerEventData eventData) => HandlerState.OnUp(eventData);

    #endregion

    #region Unity Events

    private UnityEvent<EventData> onIndexChanged = new UnityEvent<EventData>();
    public UnityEvent<EventData> OnIndexChanged { get => onIndexChanged; }

    private UnityEvent<EventData> onInteractionStarted = new UnityEvent<EventData>();
    public UnityEvent<EventData> OnInteractionStarted { get => onInteractionStarted; }

    private UnityEvent<EventData> onInteractionEnded = new UnityEvent<EventData>();
    public UnityEvent<EventData> OnInteractionEnded { get => onInteractionEnded; }

    #endregion

}
