using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LKAIROS.Assist;

public class Player : MonoBehaviour
{
    public static Player instance;
    public SpriteRenderer head, body, leftArm, ball;
    public GameObject progressBar;
    public GameObject touchPanel;
    public GameObject progressBarPivot;
    Coroutine progressBarCoroutine;
    Animator animator;
    bool isThrow = false;
    public bool debugThrowing = false;
    public GameObject chargingParticle, criticalParticle;
    public Canvas UICanvas;

    void Awake()
    {
        instance = this;
        SetCostume();
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        Time.timeScale = 1f;

        //특정 이벤트가 발생했을때 호출할 함수를 등록해주는 함수
        GlobalEventModule.AddEvent(GlobalEventModule_EventNames.OnValueChanged.CostumeChanged, SetCostume);
    }
    public void SetCostume(object args = null)
    {
        head.sprite = Resources.Load<Sprite>("Sprites/Character/" + SaveManager.data.equippedCostumeId + "/head");
        body.sprite = Resources.Load<Sprite>("Sprites/Character/" + SaveManager.data.equippedCostumeId + "/body");
        leftArm.sprite = Resources.Load<Sprite>("Sprites/Character/" + SaveManager.data.equippedCostumeId + "/leftArm");
        ball.sprite = Resources.Load<Sprite>("Sprites/Throwings/" + SaveManager.data.ballLevel);
    }

    public void Throwing()
    {
        GameObject tmp = ResourceObjectPooler.instance.Instantiate("Prefabs/BallPrf"); //오브젝트 풀링 함수
        tmp.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/Throwings/" + SaveManager.data.ballLevel.ToString());

        if (timingProgress >= 1)
        {
            tmp.transform.Find("NormalTrail").gameObject.SetActive(false);
            tmp.transform.Find("CriticalTrail").gameObject.SetActive(true);
        }
        tmp.transform.position = ball.transform.position;
        tmp.transform.rotation = ball.transform.rotation;

        //공 속도 : ballLevel
        float ballSpeed = SaveManager.data.ballLevel * SaveManager.data.ballLevel + 60;

        //공 속도 : powerLevel
        ballSpeed *= (Mathf.Pow(1.05f, SaveManager.data.powerLevel));

        //공 속도 : 환생 어빌리티
        ballSpeed *= RebirthAbilityApply.instance.GetPowerBonus();

        //공 속도 : 환생 상점
        ballSpeed *= SaveManager.data.AltagraShopAbilityList[1];

        if (SaveManager.data.boughtCostume[6])
        {
            ballSpeed *= 2;
        }

        if (SaveManager.data.summons[1] > 0)
        {
            int star = SaveManager.data.summonTranscend[1] + 1;
            ballSpeed *= (1f + 0.05f * star);
        }
        if (SaveManager.data.summons[9] > 0)
        {
            int star = SaveManager.data.summonTranscend[9] + 1;
            ballSpeed *= (1f + 0.25f * star);
        }
        if(SaveManager.data.summons[14] > 0)
        {
            int star = SaveManager.data.summonTranscend[14] + 1;
            ballSpeed *= (1f + 0.25f * star);
        }

        if (timingProgress >= 1)
        {
            ballSpeed *= 2;
        }

        ScoreBoardText.scoreMultiplier = 1;
        ScoreBoardText.instance.SetGravityOnPosition(0);
        if (ballSpeed >= 1000)
        {
            ScoreBoardText.scoreMultiplier = ballSpeed / 1000;
            ballSpeed = 1000;
        }

        tmp.GetComponent<Rigidbody2D>().velocity = new Vector3(ballSpeed, 0, 0);
        tmp.GetComponent<Rigidbody2D>().angularVelocity = -800;

        tmp.GetComponent<Ball>().Init();

        CameraScript.instance.follwingTarget = tmp;

        ScoreBoardText.instance.scoreBoard.gameObject.SetActive(true);
        QuestManager.instance.gameObject.SetActive(false);

        if (timingProgress >= 1)
        {
            SoundManager.instance.Play("Sounds/SFX/CriticalThrowing", false, false);
        }
        else
        {
            SoundManager.instance.Play("Sounds/SFX/NormalThrowing", false, false);
        }
    }

    //거리 2배 던지기
    IEnumerator CriticalDirection()
    {
        Time.timeScale = 0f;
        criticalParticle.SetActive(true);
        criticalParticle.GetComponent<ParticleSystem>().Play();
        yield return new WaitForSecondsRealtime(1f);
        Time.timeScale = 0.1f;
        yield return new WaitForSecondsRealtime(0.5f);
        Time.timeScale = 1f;
    }

    public void OnPressed()
    {
        if (isThrow)
            return;

        SoundManager.instance.Play("Sounds/SFX/Charge", true, false, 0f, "Charge");
        SoundManager.instance.SetVolumeByGroup("Charge", 0.4f);

        PlayThrowingReady();
        StartProgressBar();

        chargingParticle.SetActive(true);
    }

    public void OnUnPressd()
    {
        if (isThrow)
            return;

        SoundManager.instance.StopAllByClipPath("Sounds/SFX/Charge");
        PlayThrowing();
        isThrow = true;
        StopCoroutine(progressBarCoroutine);
        touchPanel.SetActive(false);

        chargingParticle.SetActive(false);

        if (debugThrowing && (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor))
        {
            timingProgress = 1;
        }

        if (SaveManager.data.packageBought.Contains("timing_100_package"))
        {
            AutoTimingEffectText.instance.GetComponent<EffectText>().InitPercentText(I2.Loc.LocalizationManager.GetTermTranslation("AutoTiming100"), new Color(0f, 1f, 1f));
            timingProgress = 1;
        }
        progressBarPivot.transform.localScale = new Vector3(timingProgress, 1, 1);

        if (timingProgress >= 1)
        {
            SoundManager.instance.Play("Sounds/SFX/buff2", false, false);
            StartCoroutine(CriticalDirection());
        }

        //퀘스트용 ThrowingCount
        SaveManager.data.preservedThrowingCount++;

        UICanvas.gameObject.SetActive(false);
    }

    public void PlayThrowingReady()
    {
        animator.Play("ThrowingReady", -1);
    }

    public void PlayThrowingReady2()
    {
        animator.Play("ThrowingReady2", -1);
    }

    public void PlayThrowing()
    {
        animator.Play("Throwing", -1);
    }

    public void StartProgressBar()
    {
        progressBar.SetActive(true);
        progressBarCoroutine = StartCoroutine(ProgressBarTiming());
    }

    public float progressDeltaOrigin = 1.5f;
    float timingProgress = 0;
    IEnumerator ProgressBarTiming()
    {
        float timingProgressDelta = progressDeltaOrigin;
        while (true)
        {
            timingProgress += timingProgressDelta * Time.deltaTime;
            if (timingProgress >= 1)
            {
                timingProgress = 1;
                timingProgressDelta = -progressDeltaOrigin;
            }
            else if (timingProgress <= 0)
            {
                timingProgress = 0;
                timingProgressDelta = progressDeltaOrigin;
            }
            progressBarPivot.transform.localScale = new Vector3(timingProgress, 1, 1);

            gameObject.transform.localScale = new Vector3(1 + timingProgress / 5, 1 + timingProgress / 5, 1);
            yield return null;
        }
    }
}
