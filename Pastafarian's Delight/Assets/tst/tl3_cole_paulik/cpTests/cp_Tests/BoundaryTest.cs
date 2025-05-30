using NUnit.Framework;
using UnityEngine;
using System.Collections;
using UnityEngine.TestTools;

public class AllItemTests
{
    // ------------------ Item Spawner Tests ------------------
    public class ItemSpawnerTests
    {
        private GameObject spawnerObject;
        private ItemSpawner itemSpawner;

        [SetUp]
        public void Setup()
        {
            spawnerObject = new GameObject();
            itemSpawner = spawnerObject.AddComponent<ItemSpawner>();
            itemSpawner.itemPrefab = new GameObject(); // Mock prefab
        }

        [UnityTest]
        public IEnumerator SpawnItems_WithinBounds_SpawnsSuccessfully()
        {
            itemSpawner.SpawnItems(999);
            yield return null;
            Assert.AreEqual(999, itemSpawner.transform.childCount);
        }

        [UnityTest]
        public IEnumerator SpawnItems_ExceedsLimit_StopsAtMax()
        {
            itemSpawner.limitEnabled = true;
            itemSpawner.maxItems = 1000;
            itemSpawner.SpawnItems(1001);
            yield return null;
            Assert.AreEqual(1000, itemSpawner.transform.childCount);
        }

        [TearDown]
        public void Teardown()
        {
            Object.Destroy(spawnerObject);
        }
    }

    // ------------------ Game Item Tests ------------------
    public class GameItemTests
    {
        private GameObject item;
        private GameObject player;

        [SetUp]
        public void Setup()
        {
            player = new GameObject("Player");
            player.tag = "Player";
            player.AddComponent<BoxCollider2D>().isTrigger = true;

            item = new GameObject("Item");
            item.AddComponent<BoxCollider2D>().isTrigger = true;

            var rigidbody = item.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0;
            rigidbody.isKinematic = true;

            var gameItem = item.AddComponent<GameItem>();
            var mockPowerUp = item.AddComponent<MockPowerUp>();
            gameItem.powerUpEffect = mockPowerUp;
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(item);
        }

        [UnityTest]
        public IEnumerator GameItem_CollidesWithPlayer_TriggersEffect()
        {
            item.transform.position = player.transform.position;
            yield return new WaitForFixedUpdate();

            var mock = item.GetComponent<MockPowerUp>();
            Assert.IsTrue(mock.effectApplied);
        }

        private class MockPowerUp : MonoBehaviour, IPowerUp
        {
            public bool effectApplied = false;
            public void ApplyEffect() => effectApplied = true;
        }

        public interface IPowerUp
        {
            void ApplyEffect();
        }

        public class GameItem : MonoBehaviour
        {
            public IPowerUp powerUpEffect;

            private void OnTriggerEnter2D(Collider2D other)
            {
                if (other.CompareTag("Player"))
                {
                    powerUpEffect?.ApplyEffect();
                    Destroy(gameObject);
                }
            }
        }
    }

    // ------------------ Health Boost Tests ------------------
    public class HealthBoostTests
    {
        private GameObject player;
        private PlayerHealth playerHealth;
        private GameObject healthBoostItem;
        private HealthBoost healthBoostScript;

        [SetUp]
        public void Setup()
        {
            player = new GameObject("Player");
            playerHealth = player.AddComponent<PlayerHealth>();
            playerHealth.maxHealth = 100f;
            playerHealth.currentHealth = 50f;

            healthBoostItem = new GameObject("HealthBoost");
            healthBoostScript = healthBoostItem.AddComponent<HealthBoost>();
            healthBoostScript.boostAmount = 20f;
            healthBoostItem.AddComponent<BoxCollider2D>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(healthBoostItem);
        }

        [UnityTest]
        public IEnumerator HealthBoost_InsideBounds_IncreasesHealth()
        {
            healthBoostItem.transform.position = new Vector2(5, 5);
            healthBoostScript.OnPickup(playerHealth);
            yield return null;
            Assert.AreEqual(70f, playerHealth.currentHealth);
        }

        [UnityTest]
        public IEnumerator HealthBoost_OutsideBounds_DoesNotIncreaseHealth()
        {
            healthBoostItem.transform.position = new Vector2(9999, 9999);
            float initialHealth = playerHealth.currentHealth;
            healthBoostScript.OnPickup(playerHealth);
            yield return null;
            Assert.AreEqual(initialHealth, playerHealth.currentHealth);
        }

        public class PlayerHealth : MonoBehaviour
        {
            public float maxHealth = 100f;
            public float currentHealth = 50f;
        }

        public class HealthBoost : MonoBehaviour
        {
            public float boostAmount = 20f;
            public void OnPickup(PlayerHealth playerHealth)
            {
                playerHealth.currentHealth += boostAmount;
            }
        }
    }

    // ------------------ ItemSpawner Position Tests ------------------
    public class ItemSpawnerPositionTests
    {
        private GameObject spawnerObj;
        private ItemSpawner spawner;

        [SetUp]
        public void Setup()
        {
            spawnerObj = new GameObject("Spawner");
            spawner = spawnerObj.AddComponent<ItemSpawner>();
            spawner.itemPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(spawner.itemPrefab);
            Object.DestroyImmediate(spawnerObj);

            foreach (var obj in Object.FindObjectsOfType<GameObject>())
            {
                Object.DestroyImmediate(obj);
            }
        }

        [UnityTest]
        public IEnumerator SpawnItem_InsideBounds_SpawnsItem()
        {
            Vector2 position = new Vector2(5, 5);
            spawner.SpawnItem(position);
            yield return null;

            var spawned = GameObject.Find("Cube(Clone)");
            Assert.IsNotNull(spawned);
            Assert.AreEqual((Vector2)spawned.transform.position, position);
        }

        [UnityTest]
        public IEnumerator SpawnItem_OutsideBounds_DoesNotSpawnItem()
        {
            Vector2 position = new Vector2(9999, 9999);
            spawner.bounds = new Rect(0, 0, 10, 10);

            spawner.SpawnItem(position);
            yield return null;

            var spawned = GameObject.Find("Cube(Clone)");
            Assert.IsNull(spawned);
        }
    }

    public class ItemSpawner : MonoBehaviour
    {
        public GameObject itemPrefab;
        public Rect bounds = new Rect(0, 0, 1000, 1000);
        public bool limitEnabled = false;
        public int maxItems = 100;

        public void SpawnItems(int count)
        {
            int toSpawn = limitEnabled ? Mathf.Min(count, maxItems) : count;
            for (int i = 0; i < toSpawn; i++)
            {
                var item = Instantiate(itemPrefab, transform);
            }
        }

        public void SpawnItem(Vector2 position)
        {
            if (!bounds.Contains(position))
            {
                Debug.Log("Out of bounds");
                return;
            }

            Instantiate(itemPrefab, position, Quaternion.identity);
        }
    }

    // ------------------ Speed Boost Tests ------------------
    public class SpeedBoostTests
    {
        private GameObject player;
        private PlayerMovement playerMovement;
        private GameObject speedBoostItem;
        private SpeedBoost speedBoostScript;

        [SetUp]
        public void Setup()
        {
            player = new GameObject("Player");
            playerMovement = player.AddComponent<PlayerMovement>();
            playerMovement.baseSpeed = 5f;
            playerMovement.currentSpeed = 5f;

            speedBoostItem = new GameObject("SpeedBoost");
            speedBoostScript = speedBoostItem.AddComponent<SpeedBoost>();
            speedBoostScript.boostAmount = 3f;
            speedBoostItem.AddComponent<BoxCollider2D>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(speedBoostItem);
        }

        [UnityTest]
        public IEnumerator SpeedBoost_InsideBounds_IncreasesSpeed()
        {
            speedBoostItem.transform.position = new Vector2(5, 5);
            speedBoostScript.OnPickup(playerMovement);
            yield return null;
            Assert.AreEqual(8f, playerMovement.currentSpeed);
        }

        [UnityTest]
        public IEnumerator SpeedBoost_OutsideBounds_DoesNotIncreaseSpeed()
        {
            speedBoostItem.transform.position = new Vector2(9999, 9999);
            float initialSpeed = playerMovement.currentSpeed;
            speedBoostScript.OnPickup(playerMovement);
            yield return null;
            Assert.AreEqual(initialSpeed, playerMovement.currentSpeed);
        }

        public class PlayerMovement : MonoBehaviour
        {
            public float baseSpeed = 5f;
            public float currentSpeed = 5f;
        }

        public class SpeedBoost : MonoBehaviour
        {
            public float boostAmount = 3f;
            public void OnPickup(PlayerMovement playerMovement)
            {
                playerMovement.currentSpeed += boostAmount;
            }
        }
    }
}
