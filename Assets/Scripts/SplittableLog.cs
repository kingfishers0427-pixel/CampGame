using UnityEngine;

public class SplittableLog : MonoBehaviour
{
    [Header("割れた後に出現させる薪のプレハブ")]
    public GameObject splitWoodPrefab;

    [Header("1回の薪割りで出現する本数")]
    public int spawnCount = 2;

    [Header("割れるのに必要な衝撃の強さ")]
    public float requiredImpactSpeed = 7.0f;

    [Header("刃の当たり判定の厳しさ（0.4〜0.7推奨）")]
    [Range(0.1f, 0.95f)]
    public float bladeAngleThreshold = 0.45f;

    [Header("効果音（SE）の設定")]
    [Tooltip("薪が割れて転がる音（複数登録するとランダムで鳴ります）")]
    public AudioClip[] splitSounds;
    [Range(0f, 1f)]
    public float splitVolume = 1.0f;

    [Tooltip("斧が当たったけれど割れなかった時の音（1種類）")]
    public AudioClip failHitSound;
    [Range(0f, 1f)]
    public float failVolume = 0.8f;

    [Header("音のタイミング微調整（遅延対策）")]
    [Tooltip("WAVEファイルの先頭に無音がある場合、ここで秒数を指定して飛ばせます（例: 0.05）")]
    [Range(0f, 0.5f)]
    public float soundStartOffset = 0.0f;

    [Header("調整用：判定結果をログに出す")]
    public bool showImpactLog = false;

    private bool isSplit = false;
    private float lastFailSoundTime = -1f;

    private void OnCollisionEnter(Collision collision)
    {
        if (isSplit) return;

        AxeTool axe = collision.gameObject.GetComponentInParent<AxeTool>();

        if (axe != null)
        {
            float impactSpeed = collision.relativeVelocity.magnitude;
            float downwardSpeed = -collision.relativeVelocity.y;

            Vector3 bladeDir = axe.GetWorldBladeDirection();
            ContactPoint contact = collision.GetContact(0);
            Vector3 intoLogDir = -contact.normal;

            float bladeAlignment = Vector3.Dot(bladeDir, intoLogDir);

            if (showImpactLog)
            {
                Debug.Log($"【薪割り判定】勢い: {impactSpeed:F1} | 振り下ろし: {downwardSpeed:F1} | 刃の一致度: {bladeAlignment:F2}");
            }

            if (impactSpeed >= requiredImpactSpeed && downwardSpeed > 1.0f && bladeAlignment >= bladeAngleThreshold)
            {
                SplitLog();
            }
            else if (impactSpeed >= 1.5f)
            {
                PlayFailSound(impactSpeed);
            }
        }
    }

    private void SplitLog()
    {
        isSplit = true;

        // 1. 7種類の中からランダムに1つ選んで即座に再生
        if (splitSounds != null && splitSounds.Length > 0)
        {
            AudioClip chosenClip = splitSounds[Random.Range(0, splitSounds.Length)];
            PlayInstantSound(chosenClip, splitVolume, 0.95f, 1.05f);
        }

        // 2. 割れた薪を生成
        if (splitWoodPrefab != null)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-0.15f, 0.15f), 0.1f, Random.Range(-0.15f, 0.15f));
                Vector3 spawnPos = transform.position + offset;

                GameObject newWood = Instantiate(splitWoodPrefab, spawnPos, transform.rotation);

                Rigidbody rb = newWood.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 popForce = new Vector3(Random.Range(-1.5f, 1.5f), 2.0f, Random.Range(-1.5f, 1.5f));
                    rb.AddForce(popForce, ForceMode.Impulse);
                }
            }
        }

        // 3. 元の丸太を消去
        Destroy(gameObject);
    }

    private void PlayFailSound(float impactSpeed)
    {
        if (failHitSound == null) return;

        if (Time.time - lastFailSoundTime < 0.2f) return;
        lastFailSoundTime = Time.time;

        float dynamicVolume = Mathf.Clamp01(impactSpeed / requiredImpactSpeed) * failVolume;
        PlayInstantSound(failHitSound, dynamicVolume, 0.92f, 1.08f);
    }

    // 遅延ゼロで即座に再生する処理
    private void PlayInstantSound(AudioClip clip, float volume, float minPitch, float maxPitch)
    {
        if (clip == null) return;

        // オフセット（頭出しスキップ）が不要で、MainCameraにAudioSourceがある場合は最速のPlayOneShotを使う
        if (soundStartOffset <= 0.001f && Camera.main != null)
        {
            AudioSource camAudio = Camera.main.GetComponent<AudioSource>();
            if (camAudio != null)
            {
                camAudio.pitch = Random.Range(minPitch, maxPitch);
                camAudio.PlayOneShot(clip, volume);
                return;
            }
        }

        // 頭出しスキップを使う場合、またはカメラにAudioSourceがない場合の再生
        GameObject soundObj = new GameObject("TempWoodSound");
        soundObj.transform.position = transform.position;

        AudioSource audioSource = soundObj.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0.5f;
        audioSource.pitch = Random.Range(minPitch, maxPitch);

        // 指定秒数だけ先頭の無音をスキップして再生開始
        audioSource.time = Mathf.Clamp(soundStartOffset, 0f, clip.length - 0.05f);
        audioSource.Play();

        Destroy(soundObj, (clip.length - audioSource.time) + 0.1f);
    }
}
