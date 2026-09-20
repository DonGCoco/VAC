using UnityEngine;

namespace VACExperiment
{
    public class ApparentSizeController : MonoBehaviour
    {
        [SerializeField] private float referenceDistanceMeters = 1.0f;

        private Vector3 initialScale;
        private bool initialized;

        private void Awake()
        {
            CacheInitialScale();
        }

        public void CacheInitialScale()
        {
            initialScale = transform.localScale;
            initialized = true;
        }

        public void ApplyForDistance(float currentDistanceMeters)
        {
            if (!initialized)
                CacheInitialScale();

            if (referenceDistanceMeters <= 0f)
            {
                Debug.LogError("Reference distance must be greater than zero.");
                return;
            }

            float scaleFactor = currentDistanceMeters / referenceDistanceMeters;
            transform.localScale = initialScale * scaleFactor;
        }

        public void ResetScale()
        {
            if (!initialized)
                CacheInitialScale();

            transform.localScale = initialScale;
        }
    }
}
