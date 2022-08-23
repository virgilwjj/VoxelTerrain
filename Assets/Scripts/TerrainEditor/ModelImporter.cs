using TMPro;
using UnityEngine;

namespace VoxelTerrain
{
    public class ModelImporter : MonoBehaviour
    {
        public bool IsActive = false;
        [SerializeField]
        TextMeshProUGUI _text;

        [SerializeField]
        ChunkSetting _chunkSetting;
        ChunkManager _chunkManager;

        void Awake()
        {
            _chunkManager = GetComponent<ChunkManager>();
        }

        void Update()
        {
            if (!IsActive)
            {
                return;
            }
            _text?.SetText(@"Current Mode: Model
1: Switch View Mode
2: Switch Edit Mode


mouse0: Put Model");

            PutModel();
        }

        const float maxDistance = 1024;
        void PutModel()
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, maxDistance))
                {
                    _chunkManager.PutModel(hit.point); 
                }
            }
        }

    }

}
