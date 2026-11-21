using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoheart.World
{
    /// <summary>Shared foliage meshes in spatial batches; no duplicated grass vertex buffers.</summary>
    [ExecuteAlways]
    public sealed class InstancedFoliageV12 : MonoBehaviour
    {
        [Serializable]
        private sealed class Batch
        {
            [SerializeField] private Mesh _mesh;
            [SerializeField] private Bounds _bounds;
            [SerializeField] private Vector3[] _positions;
            [SerializeField] private Quaternion[] _rotations;
            [SerializeField] private float[] _scales;

            public Mesh Mesh => _mesh;
            public Bounds Bounds => _bounds;
            public Vector3[] Positions => _positions;
            public Quaternion[] Rotations => _rotations;
            public float[] Scales => _scales;
        }

        [SerializeField] private Material _material;
        [SerializeField] private Batch[] _batches;
        private Matrix4x4[][] _matrices;
        private readonly Plane[] _planes = new Plane[6];

        private void OnEnable()
        {
            Initialize();
            if (isActiveAndEnabled) RenderPipelineManager.beginCameraRendering += RenderForCamera;
        }

        private void OnDisable() => RenderPipelineManager.beginCameraRendering -= RenderForCamera;

        private void OnValidate() => _matrices = null;

        private void Initialize()
        {
            if (_material == null || _batches == null) { _matrices = null; return; }
            _matrices = new Matrix4x4[_batches.Length][];
            for (int b = 0; b < _batches.Length; b++)
            {
                var batch = _batches[b];
                if (batch.Mesh == null || batch.Positions == null || batch.Rotations == null ||
                    batch.Scales == null || batch.Positions.Length != batch.Rotations.Length ||
                    batch.Positions.Length != batch.Scales.Length || batch.Positions.Length > 256)
                {
                    Debug.LogError("Invalid V1.2 foliage batch.", this);
                    _matrices = null;
                    enabled = false;
                    return;
                }
                _matrices[b] = new Matrix4x4[batch.Positions.Length];
                for (int i = 0; i < batch.Positions.Length; i++)
                    _matrices[b][i] = Matrix4x4.TRS(batch.Positions[i], batch.Rotations[i], Vector3.one * batch.Scales[i]);
            }
        }

        private void RenderForCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera == null || camera.cameraType == CameraType.Preview) return;
            if (_matrices == null) Initialize();
            if (_matrices == null) return;
            GeometryUtility.CalculateFrustumPlanes(camera, _planes);
            Matrix4x4 root = transform.localToWorldMatrix;
            bool identity = root == Matrix4x4.identity;
            for (int b = 0; b < _batches.Length; b++)
            {
                var batch = _batches[b];
                Bounds local = batch.Bounds;
                Vector3 e = local.extents;
                Vector3 x = root.MultiplyVector(new Vector3(e.x, 0f, 0f));
                Vector3 y = root.MultiplyVector(new Vector3(0f, e.y, 0f));
                Vector3 z = root.MultiplyVector(new Vector3(0f, 0f, e.z));
                Bounds bounds = new Bounds(root.MultiplyPoint3x4(local.center), new Vector3(
                    Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                    Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                    Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z)) * 2f);
                if (!GeometryUtility.TestPlanesAABB(_planes, bounds)) continue;
                var parameters = new RenderParams(_material)
                {
                    worldBounds = bounds,
                    camera = camera,
                    layer = gameObject.layer,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = true
                };
                // This prefab is authored at identity. A moved instance also remains correct.
                if (SystemInfo.supportsInstancing && identity)
                    Graphics.RenderMeshInstanced(parameters, batch.Mesh, 0, _matrices[b], _matrices[b].Length);
                else
                    foreach (Matrix4x4 matrix in _matrices[b])
                        Graphics.RenderMesh(parameters, batch.Mesh, 0, root * matrix);
            }
        }
    }
}
