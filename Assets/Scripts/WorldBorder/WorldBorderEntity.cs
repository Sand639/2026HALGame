using UnityEngine;

namespace MyGame
{
    /// <summary>
    /// Field の4端を基準に自動配置されるワールドボーダー
    /// BoxCollider の size / center / transform を元に4辺を生成する
    /// </summary>
    public class WorldBorderEntity : Entity
    {
        [Header("Field参照")]
        [SerializeField] private Transform fieldRoot;
        [SerializeField] private BoxCollider fieldBoxCollider;

        [Header("Border設定")]
        [SerializeField] private float borderHeight = 10.0f;
        [SerializeField] private float borderThickness = 1.0f;
        [SerializeField] private float borderYOffset = 5.0f;
        [SerializeField] private float borderInset = 0.0f;   // 内側/外側の微調整
        [SerializeField] private bool followEveryFrame = true;

        [Header("見た目")]
        [SerializeField] private Material borderMaterial;
        [SerializeField] private bool createTriggerCollider = true;

        // 4辺
        private Transform m_topBorder;
        private Transform m_bottomBorder;
        private Transform m_leftBorder;
        private Transform m_rightBorder;

        // 前回状態
        private Vector3 m_prevFieldPos;
        private Quaternion m_prevFieldRot;
        private Vector3 m_prevFieldScale;
        private Vector3 m_prevColliderCenter;
        private Vector3 m_prevColliderSize;

        //========================================================
        // ライフサイクル
        //========================================================
        public override void InitEntity()
        {
            if (fieldRoot == null)
            {
                Debug.LogError("[WorldBorderEntity] fieldRoot が設定されていません。", this);
                return;
            }

            if (fieldBoxCollider == null)
            {
                fieldBoxCollider = fieldRoot.GetComponent<BoxCollider>();
            }

            if (fieldBoxCollider == null)
            {
                Debug.LogError("[WorldBorderEntity] Field に BoxCollider が必要です。", this);
                return;
            }

            CreateBordersIfNeeded();
            RefreshBorders(force: true);
        }

        public override void UpdateEntity()
        {
            if (!m_isActive) return;
            if (fieldRoot == null || fieldBoxCollider == null) return;

            if (followEveryFrame)
            {
                RefreshBorders();
            }
            else
            {
                if (HasFieldChanged())
                {
                    RefreshBorders();
                }
            }
        }

        public override void UninitEntity()
        {
            // 必要ならここで後始末
        }

        protected override void OnEnableEntity()
        {
            RefreshBorders(force: true);
        }

        protected override void OnDisableEntity()
        {
        }

        //========================================================
        // Unityイベント
        //========================================================
        private void Start()
        {
            InitEntity();
        }

        private void Update()
        {
            UpdateActiveState();
            UpdateEntity();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (fieldRoot != null && fieldBoxCollider == null)
            {
                fieldBoxCollider = fieldRoot.GetComponent<BoxCollider>();
            }

            CreateBordersIfNeeded();
            ApplyMaterialToAllBorders();

            // エディタ上でも値変更時に見た目を更新したい場合
            if (!Application.isPlaying)
            {
                RefreshBorders(force: true);
            }
        }
#endif

        //========================================================
        // Border生成
        //========================================================
        private void CreateBordersIfNeeded()
        {
            if (m_topBorder == null) m_topBorder = CreateSingleBorder("Border_Top");
            if (m_bottomBorder == null) m_bottomBorder = CreateSingleBorder("Border_Bottom");
            if (m_leftBorder == null) m_leftBorder = CreateSingleBorder("Border_Left");
            if (m_rightBorder == null) m_rightBorder = CreateSingleBorder("Border_Right");
        }

        private Transform CreateSingleBorder(string borderName)
        {
            Transform existing = transform.Find(borderName);
            if (existing != null) return existing;

            GameObject borderObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            borderObj.name = borderName;
            borderObj.transform.SetParent(transform, false);

            // 見た目
            if (borderMaterial != null)
            {
                MeshRenderer renderer = borderObj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = borderMaterial;
                }
            }

            // Collider設定
            BoxCollider col = borderObj.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = createTriggerCollider;
            }

            return borderObj.transform;
        }

        //========================================================
        // 配置更新
        //========================================================
        private void RefreshBorders(bool force = false)
        {
            if (fieldRoot == null || fieldBoxCollider == null) return;

            if (!force && !followEveryFrame && !HasFieldChanged())
                return;

            Bounds bounds = fieldBoxCollider.bounds;

            float minX = bounds.min.x;
            float maxX = bounds.max.x;
            float minZ = bounds.min.z;
            float maxZ = bounds.max.z;

            float centerX = bounds.center.x;
            float centerZ = bounds.center.z;

            float width = bounds.size.x;
            float depth = bounds.size.z;

            float y = bounds.min.y + borderYOffset;

            // 上
            ApplyBorderTransform(
                m_topBorder,
                new Vector3(centerX, y, maxZ + borderInset),
                Quaternion.identity,
                new Vector3(width + borderThickness * 2.0f, borderHeight, borderThickness)
            );

            // 下
            ApplyBorderTransform(
                m_bottomBorder,
                new Vector3(centerX, y, minZ - borderInset),
                Quaternion.identity,
                new Vector3(width + borderThickness * 2.0f, borderHeight, borderThickness)
            );

            // 右
            ApplyBorderTransform(
                m_rightBorder,
                new Vector3(maxX + borderInset, y, centerZ),
                Quaternion.identity,
                new Vector3(borderThickness, borderHeight, depth + borderThickness * 2.0f)
            );

            // 左
            ApplyBorderTransform(
                m_leftBorder,
                new Vector3(minX - borderInset, y, centerZ),
                Quaternion.identity,
                new Vector3(borderThickness, borderHeight, depth + borderThickness * 2.0f)
            );

            CacheFieldState();
            ApplyMaterialToAllBorders();
        }

        private void ApplyBorderTransform(Transform border, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (border == null) return;

            border.position = pos;
            border.rotation = rot;
            border.localScale = scale;
        }

        //========================================================
        // 変更検知
        //========================================================
        private bool HasFieldChanged()
        {
            if (fieldRoot == null || fieldBoxCollider == null) return false;

            if (m_prevFieldPos != fieldRoot.position) return true;
            if (m_prevFieldRot != fieldRoot.rotation) return true;
            if (m_prevFieldScale != fieldRoot.lossyScale) return true;
            if (m_prevColliderCenter != fieldBoxCollider.center) return true;
            if (m_prevColliderSize != fieldBoxCollider.size) return true;

            return false;
        }

        private void CacheFieldState()
        {
            if (fieldRoot == null || fieldBoxCollider == null) return;

            m_prevFieldPos = fieldRoot.position;
            m_prevFieldRot = fieldRoot.rotation;
            m_prevFieldScale = fieldRoot.lossyScale;
            m_prevColliderCenter = fieldBoxCollider.center;
            m_prevColliderSize = fieldBoxCollider.size;
        }

        //========================================================
        // 外部公開
        //========================================================
        public void SetField(Transform newFieldRoot)
        {
            fieldRoot = newFieldRoot;
            fieldBoxCollider = (fieldRoot != null) ? fieldRoot.GetComponent<BoxCollider>() : null;
            CreateBordersIfNeeded();
            RefreshBorders(force: true);
        }

        public Transform GetTopBorder() => m_topBorder;
        public Transform GetBottomBorder() => m_bottomBorder;
        public Transform GetLeftBorder() => m_leftBorder;
        public Transform GetRightBorder() => m_rightBorder;

        private void ApplyMaterialToBorder(Transform border)
        {
            if (border == null) return;

            MeshRenderer renderer = border.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            renderer.sharedMaterial = borderMaterial;
        }

        private void ApplyMaterialToAllBorders()
        {
            ApplyMaterialToBorder(m_topBorder);
            ApplyMaterialToBorder(m_bottomBorder);
            ApplyMaterialToBorder(m_leftBorder);
            ApplyMaterialToBorder(m_rightBorder);
        }

    }
}