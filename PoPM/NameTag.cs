using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace PoPM
{
    public class NameTag : MonoBehaviour
    {
        public static Canvas PoPMUICanvas;

        public static Camera camera;

        private static GameObject _nameTagPrefab;
        public new string name;

        public int nameTagFontSize = 16;

        public GameObject textInstance;

        public float offx;
        public float offy;

        private bool _settedName;

        private Text nameTagText;

        private RectTransform textParent;

        private void Start()
        {
            RectTransform canvasTransform = PoPMUICanvas.GetComponent<RectTransform>();

            textInstance = Instantiate(_nameTagPrefab, canvasTransform.transform);

            nameTagText = textInstance.GetComponentsInChildren<Text>()[0];
            nameTagText.resizeTextForBestFit = true;
            nameTagText.resizeTextMaxSize = nameTagFontSize;
            nameTagText.resizeTextMinSize = nameTagFontSize - 10;

            textParent = textInstance.GetComponent<RectTransform>();
            textParent.SetParent(canvasTransform, false);
        }

        private void Update()
        {
            if (!_settedName &&
                nameTagText.text == "GenericUsername123") //TODO: Set the default text in the bundle empty
            {
                _settedName = true;
                nameTagText.text = name;
                Plugin.Logger.LogInfo($"Created nametag for {name}");
            }

            Vector3 currentPos = gameObject.transform.position + (Vector3.up * 1.8f);
            Vector3 wtsVector = camera.WorldToScreenPoint(currentPos);

            textInstance.SetActive(wtsVector.z > 0);

            var localPosition = textParent.localPosition;
            textParent.localPosition = new Vector3((wtsVector.x - Screen.width / 2), (wtsVector.y - Screen.height / 2),
                localPosition.z);
        }

        public static void CreateCanvas()
        {
            GameObject menuCanvas = GameObject.Find("Menu/Canvas");
            PoPMUICanvas = Instantiate(menuCanvas, menuCanvas.transform.parent).GetComponent<Canvas>();
            PoPMUICanvas.enabled = true;
            PoPMUICanvas.renderMode = RenderMode.ScreenSpaceCamera;
            PoPMUICanvas.planeDistance = 0.1f;

            camera = Camera.main;
            PoPMUICanvas.worldCamera = camera;

            for (int x = 0; x < PoPMUICanvas.transform.childCount; x++)
            {
                DestroyImmediate(PoPMUICanvas.transform.GetChild(x).gameObject);
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