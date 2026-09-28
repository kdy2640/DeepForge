using UnityEngine;

public sealed class BaseCamp : MonoBehaviour
{
    [SerializeField] private Transform terrainContact;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private PlayerController player;

    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == player.GetComponent<Rigidbody>())
            player.GetComponent<PlayerReturn>().ResetAtCamp();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody == player.GetComponent<Rigidbody>())
            player.GetComponent<PlayerReturn>().BeginTracking();
    }

    public void PlaceAndSpawn(TerrainManager terrain)
    {
        TerrainData data = terrain.Data;
        float centerX = data.Width * 0.5f;
        int leftX = Mathf.FloorToInt(centerX);
        int rightX = Mathf.CeilToInt(centerX);
        float threshold = terrain.Settings.Density.DensityThreshold;

        // +Z 끝 중앙에서 가장 위에 있는 실제 표면을 찾는다.
        for (int y = data.DensityFieldHeight - 1; y >= 0; y--)
        {
            float lower = Mathf.Lerp(
                data.GetDensity(new Vector3Int(leftX, y, data.Width)),
                data.GetDensity(new Vector3Int(rightX, y, data.Width)), centerX - leftX);
            float upper = Mathf.Lerp(
                data.GetDensity(new Vector3Int(leftX, y + 1, data.Width)),
                data.GetDensity(new Vector3Int(rightX, y + 1, data.Width)), centerX - leftX);
            if (lower <= threshold || upper > threshold)
                continue;

            float surfaceY = y + (threshold - lower) / (upper - lower);
            Vector3 contactPosition = terrain.transform.TransformPoint(
                new Vector3(centerX, surfaceY, data.Width) * data.Resolution);
            transform.rotation = terrain.transform.rotation;
            transform.position += contactPosition - terrainContact.position;

            Rigidbody body = player.GetComponent<Rigidbody>();
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            body.position = spawnPoint.position;
            body.rotation = spawnPoint.rotation;
            return;
        }

        throw new System.InvalidOperationException("기지 접점인 지형 +Z 끝 중앙에 표면이 없습니다.");
    }
}
