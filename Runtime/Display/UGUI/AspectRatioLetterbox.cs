using UnityEngine;
using UnityEngine.UI;

namespace GameJamKit.Display.UGUI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class AspectRatioLetterbox : MaskableGraphic
    {
        [SerializeField] private AspectRatioController _controller;
        [Tooltip("Optional root whose anchors should be restricted to the visible viewport.")]
        [SerializeField] private RectTransform _contentRoot;
        [Tooltip("Automatically match width or height so the visible viewport keeps the reference resolution scale.")]
        [SerializeField] private bool _syncCanvasScaler = true;
        [SerializeField] private CanvasScaler _canvasScaler;
        [SerializeField] private bool _keepAsLastSibling = true;

        private Vector2 _originalAnchorMin;
        private Vector2 _originalAnchorMax;
        private Vector2 _originalOffsetMin;
        private Vector2 _originalOffsetMax;
        private bool _hasOriginalContentLayout;

#if UNITY_EDITOR
        private bool _editorRefreshScheduled;
#endif

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            ResolveController();
            ResolveCanvasScaler();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResolveController();
            ResolveCanvasScaler();
            CacheContentLayout();
            Subscribe();

            if (_keepAsLastSibling)
            {
                transform.SetAsLastSibling();
            }

            Refresh(_controller != null
                ? _controller.CurrentViewport
                : new Rect(0f, 0f, 1f, 1f));
        }

        protected override void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= ApplyEditorPreview;
            _editorRefreshScheduled = false;
#endif
            Unsubscribe();
            RestoreContentLayout();
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            raycastTarget = false;

            if (!isActiveAndEnabled)
            {
                return;
            }

            ResolveController();
            ResolveCanvasScaler();

            if (Application.isPlaying)
            {
                Refresh(_controller != null
                    ? _controller.CurrentViewport
                    : new Rect(0f, 0f, 1f, 1f));
            }
            else
            {
                SetVerticesDirty();
            }
        }
#endif

        public void SetController(AspectRatioController controller)
        {
            Unsubscribe();
            _controller = controller;
            Subscribe();
            Refresh(_controller != null
                ? _controller.CurrentViewport
                : new Rect(0f, 0f, 1f, 1f));
        }

        public void SetContentRoot(RectTransform contentRoot)
        {
            RestoreContentLayout();
            _contentRoot = contentRoot;
            CacheContentLayout();
            ApplyContentViewport(_controller != null
                ? _controller.CurrentViewport
                : new Rect(0f, 0f, 1f, 1f));
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect viewport = _controller != null
                ? _controller.CurrentViewport
                : new Rect(0f, 0f, 1f, 1f);

            Rect bounds = rectTransform.rect;
            float left = Mathf.Lerp(bounds.xMin, bounds.xMax, viewport.xMin);
            float right = Mathf.Lerp(bounds.xMin, bounds.xMax, viewport.xMax);
            float bottom = Mathf.Lerp(bounds.yMin, bounds.yMax, viewport.yMin);
            float top = Mathf.Lerp(bounds.yMin, bounds.yMax, viewport.yMax);

            AddQuad(vertexHelper, new Rect(
                bounds.xMin,
                bounds.yMin,
                left - bounds.xMin,
                bounds.height));
            AddQuad(vertexHelper, new Rect(
                right,
                bounds.yMin,
                bounds.xMax - right,
                bounds.height));
            AddQuad(vertexHelper, new Rect(
                left,
                bounds.yMin,
                right - left,
                bottom - bounds.yMin));
            AddQuad(vertexHelper, new Rect(
                left,
                top,
                right - left,
                bounds.yMax - top));
        }

        private void Refresh(Rect viewport)
        {
            SetVerticesDirty();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ScheduleEditorPreview();
                return;
            }
#endif

            ApplyContentViewport(viewport);
            ApplyCanvasScaling(viewport);
        }

#if UNITY_EDITOR
        private void ScheduleEditorPreview()
        {
            if (_editorRefreshScheduled)
            {
                return;
            }

            _editorRefreshScheduled = true;
            UnityEditor.EditorApplication.delayCall += ApplyEditorPreview;
        }

        private void ApplyEditorPreview()
        {
            _editorRefreshScheduled = false;

            if (this == null || Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            ResolveController();
            ResolveCanvasScaler();

            Rect viewport = _controller != null
                ? _controller.CurrentViewport
                : new Rect(0f, 0f, 1f, 1f);

            ApplyContentViewport(viewport);
            ApplyCanvasScaling(viewport);
            SetVerticesDirty();
        }
#endif

        private void ApplyContentViewport(Rect viewport)
        {
            if (_contentRoot == null || _contentRoot == rectTransform)
            {
                return;
            }

            _contentRoot.anchorMin = viewport.min;
            _contentRoot.anchorMax = viewport.max;
            _contentRoot.offsetMin = Vector2.zero;
            _contentRoot.offsetMax = Vector2.zero;
        }

        private void CacheContentLayout()
        {
            if (_contentRoot == null || _contentRoot == rectTransform)
            {
                _hasOriginalContentLayout = false;
                return;
            }

            _originalAnchorMin = _contentRoot.anchorMin;
            _originalAnchorMax = _contentRoot.anchorMax;
            _originalOffsetMin = _contentRoot.offsetMin;
            _originalOffsetMax = _contentRoot.offsetMax;
            _hasOriginalContentLayout = true;
        }

        private void RestoreContentLayout()
        {
            if (!_hasOriginalContentLayout || _contentRoot == null)
            {
                return;
            }

            _contentRoot.anchorMin = _originalAnchorMin;
            _contentRoot.anchorMax = _originalAnchorMax;
            _contentRoot.offsetMin = _originalOffsetMin;
            _contentRoot.offsetMax = _originalOffsetMax;
            _hasOriginalContentLayout = false;
        }

        private void ResolveController()
        {
            if (_controller == null)
            {
                _controller = FindFirstObjectByType<AspectRatioController>();
            }
        }

        private void ResolveCanvasScaler()
        {
            if (_canvasScaler == null)
            {
                _canvasScaler = GetComponentInParent<CanvasScaler>();
            }
        }

        private void ApplyCanvasScaling(Rect viewport)
        {
            if (!_syncCanvasScaler || _canvasScaler == null ||
                _canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                _canvasScaler.screenMatchMode !=
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
            {
                return;
            }

            if (viewport.width < 0.9999f)
            {
                _canvasScaler.matchWidthOrHeight = 1f;
            }
            else if (viewport.height < 0.9999f)
            {
                _canvasScaler.matchWidthOrHeight = 0f;
            }
        }

        private void Subscribe()
        {
            if (_controller != null)
            {
                _controller.ViewportChanged -= Refresh;
                _controller.ViewportChanged += Refresh;
            }
        }

        private void Unsubscribe()
        {
            if (_controller != null)
            {
                _controller.ViewportChanged -= Refresh;
            }
        }

        private void AddQuad(VertexHelper vertexHelper, Rect quad)
        {
            if (quad.width <= 0f || quad.height <= 0f)
            {
                return;
            }

            int startIndex = vertexHelper.currentVertCount;
            Color32 vertexColor = color;

            vertexHelper.AddVert(new Vector3(quad.xMin, quad.yMin), vertexColor, Vector2.zero);
            vertexHelper.AddVert(new Vector3(quad.xMin, quad.yMax), vertexColor, Vector2.zero);
            vertexHelper.AddVert(new Vector3(quad.xMax, quad.yMax), vertexColor, Vector2.zero);
            vertexHelper.AddVert(new Vector3(quad.xMax, quad.yMin), vertexColor, Vector2.zero);
            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            color = Color.black;
            raycastTarget = false;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
#endif
    }
}
