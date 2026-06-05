using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace UI.Popups
{
    public class UpdatePopupController : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject updatePopupRoot;
        [SerializeField] private GameObject dimBackground;

        [Header("Header")]
        [SerializeField] private Image updateThumbnail;
        [SerializeField] private Image updateBadge;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Transform featureListContainer;
        [SerializeField] private GameObject featureItemPrefab;

        [Header("Buttons")]
        [SerializeField] private Button laterButton;
        [SerializeField] private Button updateNowButton;

        private string _storeUrl;
        private UniTaskCompletionSource _dismissTcs;

        private void Awake()
        {
            if (updateNowButton != null) updateNowButton.onClick.AddListener(OnUpdateClicked);
            if (laterButton != null) laterButton.onClick.AddListener(OnLaterClicked);
        }

        private void OnDestroy()
        {
            if (updateNowButton != null) updateNowButton.onClick.RemoveListener(OnUpdateClicked);
            if (laterButton != null) laterButton.onClick.RemoveListener(OnLaterClicked);
            _dismissTcs?.TrySetCanceled();
        }

        public void Setup(bool isForceUpdate, string version, string title, string description, List<string> features, string storeUrl, string thumbnailUrl = null, CancellationToken ct = default)
        {
            _storeUrl = storeUrl;

            if (titleText != null) titleText.text = title;
            if (versionText != null) versionText.text = $"Version {version}";
            if (descriptionText != null) descriptionText.text = description;

            // Handle Buttons
            if (laterButton != null)
                laterButton.gameObject.SetActive(!isForceUpdate);

            // Handle Features
            ClearFeatures();
            if (features != null)
            {
                foreach (var feature in features)
                {
                    AddFeature(feature);
                }
            }

            // Handle Thumbnail (downloaded from remote config link)
            if (!string.IsNullOrEmpty(thumbnailUrl))
                LoadThumbnailAsync(thumbnailUrl, ct).Forget();

            if (updatePopupRoot != null)
                updatePopupRoot.SetActive(true);
        }

        private void AddFeature(string description)
        {
            if (featureItemPrefab == null || featureListContainer == null) return;
            
            GameObject item = Instantiate(featureItemPrefab, featureListContainer);
            var featureItem = item.GetComponent<UpdatePopupFeatureItem>();
            if (featureItem != null)
            {
                featureItem.SetDescription(description);
            }
        }

        private async UniTaskVoid LoadThumbnailAsync(string url, CancellationToken ct)
        {
            if (updateThumbnail == null) return;

            using var request = UnityWebRequestTexture.GetTexture(url);
            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            }
            catch (System.OperationCanceledException) { return; }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UpdatePopup] Thumbnail download failed: {ex.Message}");
                return;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[UpdatePopup] Thumbnail download failed: {request.error}");
                return;
            }

            if (updateThumbnail == null) return;

            var texture = DownloadHandlerTexture.GetContent(request);
            updateThumbnail.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }

        private void ClearFeatures()
        {
            if (featureListContainer == null) return;
            foreach (Transform child in featureListContainer)
            {
                Destroy(child.gameObject);
            }
        }

        public async UniTask AwaitDismissAsync(CancellationToken ct)
        {
            _dismissTcs = new UniTaskCompletionSource();
            await _dismissTcs.Task.AttachExternalCancellation(ct);
        }

        public void OnUpdateClicked()
        {
            if (!string.IsNullOrEmpty(_storeUrl))
                Application.OpenURL(_storeUrl);
        }

        public void OnLaterClicked()
        {
            Hide();
            _dismissTcs?.TrySetResult();
        }

        public void Hide()
        {
            if (updatePopupRoot != null)
                updatePopupRoot.SetActive(false);
        }
    }
}
