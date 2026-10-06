using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MeadowQuest
{
    public sealed class EndingSequence : MonoBehaviour
    {
        public Text heading, credits;
        public RectTransform viewport;
        public Button confirm;
        public string creatorName = "";
        Action completed;
        int phase;
        Coroutine running;
        public void Begin(Action onCompleted)
        {
            gameObject.SetActive(true);
            completed = onCompleted;
            phase = 0;
            credits.gameObject.SetActive(false);
            heading.text = "ゲームクリア！\n師範アサギに勝利しました！";
            Caption("クレジットへ");
            running = StartCoroutine(WaitForCredits());
        }

        void Caption(string text)
        {
            confirm.GetComponentInChildren<Text>().text = text;
            if (UnityEngine.EventSystems.EventSystem.current)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(confirm.gameObject);
        }

        IEnumerator WaitForCredits()
        {
            yield return new WaitForSecondsRealtime(3);
            StartCredits();
        }

        public void Confirm()
        {
            if (phase == 0)
                StartCredits();
            else if (phase == 1)
                FinishCredits();
            else
            {
                var callback = completed;
                completed = null;
                gameObject.SetActive(false);
                callback?.Invoke();
            }
        }

        void StartCredits()
        {
            if (running != null)
                StopCoroutine(running);
            phase = 1;
            heading.text = "Meadow Quest";
            credits.gameObject.SetActive(true);
            credits.text = "Meadow Quest\n\n" + (string.IsNullOrWhiteSpace(creatorName) ? "" : ("制作\n" + creatorName + "\n\n")) + "モンスター・地形・建物\nIncludes Guardian Monsters Artwork\nby Georg Eckert / lucidtanooki\nCC BY 4.0\ngithub.com/limbusdev/guardian_monsters_artwork\n\n人物素材\nCustomizable Character Pack\nOrdinary Bumblebee / CC0 1.0\nordinary-bumblebee.itch.io\n\n日本語フォント\nNoto Sans CJK JP\nSIL Open Font License 1.1\n\n開発・背面画像の制作支援\nOpenAI Codex / 画像生成AI\n\n遊んでくれてありがとう！";
            Caption("スキップ");
            running = StartCoroutine(Scroll());
        }

        IEnumerator Scroll()
        {
            Canvas.ForceUpdateCanvases();
            float height = Mathf.Max(credits.preferredHeight + 40, 700);
            credits.rectTransform.sizeDelta = new Vector2(0, height);
            float end = viewport.rect.height + height + 40;
            for (float time = 0; time < 28; time += Time.unscaledDeltaTime)
            {
                credits.rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(-40, end, time / 28));
                yield return null;
            }

            FinishCredits();
        }

        void FinishCredits()
        {
            if (running != null)
                StopCoroutine(running);
            phase = 2;
            credits.gameObject.SetActive(false);
            heading.text = "遊んでくれてありがとう！\n引き続き探索できます。\n終了前にメニューからセーブしてね。";
            Caption("冒険に戻る");
        }

        void OnDisable()
        {
            StopAllCoroutines();
            running = null;
        }
    }
}
