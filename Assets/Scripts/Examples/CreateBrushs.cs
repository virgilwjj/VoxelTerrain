using UnityEngine;

namespace VoxelTerrain
{
    public class CreateBrushs : MonoBehaviour
    {
        BrushTools _brushTools;

        void Awake()
        {
            _brushTools = GetComponent<BrushTools>();
        }

        void Start()
        {
            for (var numVoxelsPerAxis = 2; numVoxelsPerAxis < 15; ++numVoxelsPerAxis)
            {
                var brush = _brushTools.BuildBrush(numVoxelsPerAxis, BrushType.Sphere);
                string brushName = "sphereBrush_" + numVoxelsPerAxis;
                _brushTools.SaveBrush(brush, brushName);
            }

            for (var numVoxelsPerAxis = 2; numVoxelsPerAxis < 15; ++numVoxelsPerAxis)
            {
                var brush = _brushTools.BuildBrush(numVoxelsPerAxis, BrushType.Cube);
                string brushName = "cubeBrush_" + numVoxelsPerAxis;
                _brushTools.SaveBrush(brush, brushName);
            }
        }

    }

}
