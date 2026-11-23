using UnityEngine;

namespace Echoheart.World
{
    /// <summary>Opts the authored chests into lid-synchronized reward visibility.</summary>
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    public sealed class ChestRevealGateV120 : MonoBehaviour
    {
        [SerializeField] private SecretChestV15[] _chests;
        private bool[] _before;
        private void Awake()
        {
            if(_chests==null||_chests.Length==0){Debug.LogError("V1.20 chest reveal bindings missing.",this);enabled=false;return;}
            foreach(SecretChestV15 chest in _chests)if(chest==null){Debug.LogError("V1.20 chest reveal has an empty binding.",this);enabled=false;return;}
            _before=new bool[_chests.Length];for(int i=0;i<_chests.Length;i++)_before[i]=_chests[i].RevealAfterLid;
        }
        private void OnEnable(){if(_before!=null)foreach(SecretChestV15 chest in _chests)if(chest!=null)chest.SetRevealAfterLid(true);}
        private void OnDisable(){if(_before!=null)for(int i=0;i<_chests.Length;i++)if(_chests[i]!=null)_chests[i].SetRevealAfterLid(_before[i]);}
    }
}
