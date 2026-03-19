using MyGame;
using System.Collections.Generic;
using UnityEngine;
using Entity = MyGame.Entity;

namespace MyGame
{
    public class EntityManager : MonoBehaviour
    {
        // シングルトンのインスタンス  
        public static EntityManager Instance { get; private set; }

        // EntityList
        private List<Entity> Entities = new List<Entity>();

        //********************************************************
        // ライフサイクル Entityクラスをポリモーフィズムで自動化
        //********************************************************

        void Awake()
        {
            // シングルトンのインスタンスを設定
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // シーン内の全てのEntityを自動登録
            var entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);
            for (int i = 0; i < entities.Length; i++)
            {
                AddEntity(entities[i]);
            }

        }

        void Start()
        {

        }

        void Update()
        {
            // Entityの状態更新
            foreach (Entity entity in Entities)
            {
                entity.UpdateActiveState();
            }

            foreach (Entity entity in Entities)
            {
                // アクティブでないEntityはスキップ
                if (!entity.IsActive) continue;

                entity.UpdateEntity();
            }
        }

        void FixedUpdate()
        {
            foreach (Entity entity in Entities)
            {
                // アクティブでないEntityはスキップ
                if (!entity.IsActive) continue;

                entity.FixedUpdateEntity();
            }
        }

        void LateUpdate()
        {
            foreach (Entity entity in Entities)
            {
                // アクティブでないEntityはスキップ
                if (!entity.IsActive) continue;

                entity.LateUpdateEntity();
            }

            // 非アクティブなEntityの状態更新
            foreach (Entity entity in Entities)
            {
                if (entity.IsActive) continue;
                entity.UpdateActiveState();
            }

            // 破棄フラグが立っているEntityを削除
            for (int i = 0; i < Entities.Count; i++)
            {
                Entity entity = Entities[i];
                if (entity.IsDestroy)
                {
                    RemoveEntity(entity);
                    i--; // リストが変更されたのでインデックスを調整
                }
            }
        }

        //********************************************************
        // Entity管理
        //********************************************************

        /// <summary>
        /// Entityを追加
        /// </summary>
        /// <param name="entity">追加するEntity</param>
        public void AddEntity(Entity entity)
        {
            if (entity == null) return;
            if (Entities.Contains(entity)) return;
            Entities.Add(entity);
            entity.InitEntity();
        }

        /// <summary>
        /// Entityを削除
        /// </summary>
        /// <param name="entity">削除するEntity</param>
        public void RemoveEntity(Entity entity)
        {
            if (entity == null) return;
            entity.UninitEntity();
            Entities.Remove(entity);
            Destroy(entity.gameObject);
        }

        /// <summary>
        /// 全てのEntityを削除
        /// </summary>
        public void ClearEntities()
        {
            foreach (Entity entity in Entities)
            {
                entity.UninitEntity();
            }
            Entities.Clear();
        }
    }

}

