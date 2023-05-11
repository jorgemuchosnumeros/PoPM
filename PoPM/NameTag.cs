using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace PoPM
{
    public class NameTag : MonoBehaviour
    {
        public static Canvas PoPmuiCanvas;

        public static Camera Camera;

        private static GameObject _nameTagPrefab;

        public string nameTagText;

        public int nameTagFontSize = 16;

        public GameObject textInstance;

        private Text _nameTagText;

        private bool _setName;

        private RectTransform _textParent;

        private void Start()
        {
            RectTransform canvasTransform = PoPmuiCanvas.GetComponent<RectTransform>();

            textInstance = Instantiate(_nameTagPrefab, canvasTransform.transform);

            _nameTagText = textInstance.GetComponentsInChildren<Text>()[0];
            _nameTagText.resizeTextForBestFit = true;
            _nameTagText.resizeTextMaxSize = nameTagFontSize;
            _nameTagText.resizeTextMinSize = nameTagFontSize - 10;

            _textParent = textInstance.GetComponent<RectTransform>();
            _textParent.SetParent(canvasTransform, false);
        }

        private void Update()
        {
            if (gameObject == null)
            {
                Plugin.Logger.LogInfo("Test1");
                Destroy(this);
            }

            if (!_setName &&
                _nameTagText.text == "GenericUsername123") //FIXME: Set the default text in the bundle to empty string
            {
                _setName = true;
                _nameTagText.text = nameTagText;
                Plugin.Logger.LogInfo($"Created nametag for {nameTagText}");
            }

            Vector3 currentPos = gameObject.transform.position + (Vector3.up * 1.8f);
            Vector3 wtsVector = Camera.WorldToScreenPoint(currentPos);

            textInstance.SetActive(wtsVector.z > 0);

            var localPosition = _textParent.localPosition;
            _textParent.localPosition = new Vector3((wtsVector.x - Screen.width / 2), (wtsVector.y - Screen.height / 2),
                localPosition.z);
        }

        public static void CreateCanvas()
        {
            GameObject menuCanvas = GameObject.Find("Menu/Canvas");
            PoPmuiCanvas = Instantiate(menuCanvas, menuCanvas.transform.parent).GetComponent<Canvas>();
            PoPmuiCanvas.enabled = true;
            PoPmuiCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            PoPmuiCanvas.planeDistance = 0.1f;

            Camera = Camera.main;
            PoPmuiCanvas.worldCamera = Camera;

            for (int x = 0; x < PoPmuiCanvas.transform.childCount; x++)
            {
                DestroyImmediate(PoPmuiCanvas.transform.GetChild(x).gameObject);
            }
        }

        public static IEnumerator LoadAssetBundle(Stream path)
        {
            var bundleLoadRequest = AssetBundle.LoadFromStreamAsync(path);
            yield return bundleLoadRequest;

            var nameTagsAssetBundle = bundleLoadRequest.assetBundle;
            if (nameTagsAssetBundle == null)
            {
                Plugin.Logger.LogError("Failed to load nameTagsAssetBundle");
                yield break;
            }

            var assetLoadRequest = nameTagsAssetBundle.LoadAssetAsync<GameObject>("Container");
            yield return assetLoadRequest;

            _nameTagPrefab = assetLoadRequest.asset as GameObject;
            Plugin.Logger.LogInfo(_nameTagPrefab);
            nameTagsAssetBundle.Unload(false);
        }
    }
}