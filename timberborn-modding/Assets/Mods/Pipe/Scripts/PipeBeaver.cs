using UnityEngine;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class PipeBeaver : MonoBehaviour
{
  [SerializeField]
  public GameObject BeaverPrefab;

  private GameObject Beaver = null;

  private float initialPosition;

  private float finalPosition;

  private int state = 0;

  private float upLimit = 0.35f;

  private static int randomChance = 1;

  static public bool GetRandomChance()
  {
    return Random.value <= randomChance / 100;
  }

  public void WildBeaverAppears()
  {
    ModUtils.Log($"Beaver.click Beaver={Beaver != null}");
    if (Beaver == null && BeaverPrefab != null)
    {
      Beaver = Instantiate(BeaverPrefab, new Vector3(0, 0, 0), Quaternion.identity);
      initialPosition = Beaver.transform.position.z;
      finalPosition = initialPosition + upLimit;
      ModUtils.Log($"Beaver.click Beaver={Beaver != null} created");
    }
  }

  public void FixedUpdate()
  {
    ModUtils.Log($"Beaver.update Beaver={Beaver != null} moving");
    if (Beaver == null)
      return;
    if (state == 0 && Beaver.transform.position.z < finalPosition)
    {
      Beaver.transform.Translate(Vector3.up * Time.deltaTime, Space.Self);
    }
    else if (Beaver.transform.position.z > initialPosition)
    {
      state = 1;
      Beaver.transform.Translate(Vector3.down * Time.deltaTime, Space.Self);
    }
    else
    {
      state = 0;
      Destroy(Beaver);
    }
  }
}
}
