using UnityEngine;
using UnityEngine.UI;

namespace Naninovel
{
    public class AlphaThreshold : MonoBehaviour
    {
        [SerializeField] private Image graphic;
        [SerializeField] private float minimumThreshold = 0.1f;

        private void Start ()
        {
            if (!graphic) graphic = GetComponent<Image>();
            if (graphic) graphic.alphaHitTestMinimumThreshold = minimumThreshold;
        }
    }
}
