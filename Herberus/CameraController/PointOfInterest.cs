using UnityEngine;
using System.Collections;

/// <summary>
/// PointOfInterest allows camera to focus on them.
/// To do that, they allow to get the exact position the camera should focus on and the importance to focus on it.
/// </summary>
public interface PointOfInterest {

    /// <summary>Gets the point of interest position. It is where the camera should try to look at.</summary>
    /// <returns>The position of the point of interest.</returns>
    Vector3 getPositionOfInterest();

    /// <summary>Gets the weight of the POI. Higher values attracts the camera more.</summary>
    /// <returns>The weight of the point of interest.</returns>
    float getInterestWeight();

    /// <summary>Returns the offset that is applied when the camera enters the point of interest. </summary>
    /// <returns>The offset to give to the camera. Camera will apply it as an average of all offsets.</returns>
    Vector3 getCamOffset();

    /// <summary>Returns the offset that is applied to the moveTime of the camera.</summary>
    /// <returns>The offset to give to the move. Camera will apply it as an average of all offsets.</returns>
    float getCamMoveTimeOffset();

    /// <summary>Returns the offset that is applied when the camera DistanceChangeSpeed. </summary>
    /// <returns>The offset to give to the camera. Camera will apply it as an average of all offsets.</returns>
    float getCamDistanceSpeedOffset();

    /// <summary>Returns the rotation offset that should be applied to the camera.</summary>
    /// <returns>The rotation offset to give to the camera. Camera will apply it as an average of all offsets.</returns>
    Vector3 getCamEulerRotationOffset();

    /// <summary>Returns the rotation speed offset of the camera. Positive values increases camera speed, while negative slow it down.</summary>
    /// <returns>The rotation speed offset to give to the camera. Camera will apply it as an average of all offsets.</returns>
    float getCamRotationSpeedOffset();

    /// <summary>Returns the minium camera distance offset that will be applied to the camera. Positive values increases camera distance, while negative values will reduce it.</summary>
    /// <returns>The minimum camera distance offset to give to the camera. Camera will apply it as an average of all offsets.</returns>
    float getMinCamDistanceOffset();

    /// <summary>Returns the maxium camera distance offset that will be applied to the camera. Positive values increases camera distance, while negative values will reduce it.</summary>
    /// <returns>The maximum camera distance offset to give to the camera. Camera will apply it as an average of all offsets.</returns>
    float getMaxCamDistanceOffset();
}