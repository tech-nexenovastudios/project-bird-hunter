using UnityEngine;
using UnityEngine.VFX;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Gameplay.VFX
{
    /// <summary>
    /// Attached to a VFX Graph prefab to automatically return it to the PoolManager 
    /// after a specified duration once it is spawned. Supports passing a mesh for sampling.
    /// </summary>
    [RequireComponent(typeof(VisualEffect))]
    public class VFXGraphAutoReturn : MonoBehaviour, IPoolable
    {
        [SerializeField] private float duration = 3f;
        [SerializeField] private string meshPropertyName = "Mesh";
        
        private VisualEffect _vfx;
        private CancellationTokenSource _cts;

        private void Awake()
        {
            _vfx = GetComponent<VisualEffect>();
        }

        /// <summary>
        /// Optionally set a mesh for the VFX to sample from. 
        /// Should be called immediately after PoolManager.Get().
        /// </summary>
        public void SetMesh(Mesh mesh)
        {
            if (mesh != null)
            {
                if (_vfx.HasMesh(meshPropertyName))
                {
                    _vfx.SetMesh(meshPropertyName, mesh);
                }

                // Also support standard Particle Systems
                var ps = GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Mesh;
                    shape.mesh = mesh;
                }
            }
        }

        public void OnPoolSpawned()
        {
            _vfx.Reinit();
            _vfx.Play();
            
            // Cancel any previous return timer
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            
            AutoReturnAsync(_cts.Token).Forget();
        }

        public void OnPoolDespawned()
        {
            _cts?.Cancel();
            _vfx.Stop();
        }

        private async UniTaskVoid AutoReturnAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay((int)(duration * 1000), cancellationToken: ct);
                PoolManager.Return(gameObject);
            }
            catch (System.OperationCanceledException)
            {
                // Normal cancellation when despawned or destroyed
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
