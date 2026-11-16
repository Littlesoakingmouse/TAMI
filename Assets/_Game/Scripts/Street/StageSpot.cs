using UnityEngine;
using UnityEngine.EventSystems;

namespace Tami
{
    /// <summary>
    /// Sân khấu (đặt sẵn trong scene, prefab Stage). Bấm Biểu diễn để lượng khách x2 trong thời gian
    /// của tiết mục, sau đó hồi chiêu (GDD mục 4.9).
    /// </summary>
    public class StageSpot : MonoBehaviour, IPointerClickHandler, IInteractable
    {
        [Tooltip("Hình nghệ sĩ đang diễn (con của sân khấu)")]
        public SpriteRenderer performer;
        [Tooltip("Chữ trạng thái dưới sân khấu")]
        public TextMesh label;

        public int Index { get; private set; }
        public PerformerData Current { get; private set; }
        public float PerformUntil { get; private set; } = -1f;
        public float CooldownUntil { get; private set; } = -1f;

        public bool IsPerforming => Time.time < PerformUntil;
        public bool Ready => Time.time >= CooldownUntil;
        public float PerformLeft => Mathf.Max(0f, PerformUntil - Time.time);
        public float CooldownLeft => Mathf.Max(0f, CooldownUntil - Time.time);
        public string DisplayName => "Sân khấu " + (Index + 1);

        Vector3 performerBase;
        float noteTimer, labelTimer, t;

        void Awake()
        {
            if (performer != null)
            {
                performerBase = performer.transform.localPosition;
                performer.enabled = false;
            }
        }

        public void Init(int index) => Index = index;

        public bool Perform(PerformerData p)
        {
            var g = Game.I;
            if (p == null || !Ready || !g.Street.OwnsPerformer(p)) return false;
            Current = p;
            PerformUntil = Time.time + p.duration;
            CooldownUntil = Time.time + g.config.performCooldown;
            performer.sprite = p.sprite;
            g.Data.stats.performances++;
            g.Audio.Play(Sfx.Gong);
            g.UI.Toast(p.displayName + " bắt đầu! Khách kéo đến đông gấp đôi.");
            g.Street.NotifyChanged();
            return true;
        }

        public void ResetCooldown()
        {
            CooldownUntil = Time.time;
            Game.I.Street.NotifyChanged();
        }

        public bool TryResetWithGems()
        {
            if (Ready || !Game.I.Economy.TrySpendGems(Game.I.config.performResetGems)) return false;
            ResetCooldown();
            return true;
        }

        void Update()
        {
            bool on = IsPerforming;
            performer.enabled = on;
            if (on)
            {
                t += Time.deltaTime;
                performer.transform.localPosition = performerBase
                    + new Vector3(Mathf.Sin(t * 2f) * 0.4f, Mathf.Abs(Mathf.Sin(t * 6f)) * 0.15f, 0f);
                noteTimer -= Time.deltaTime;
                if (noteTimer <= 0f)
                {
                    noteTimer = 0.6f;
                    FloatingText.Spawn(transform.position + new Vector3(Random.Range(-1.4f, 1.4f), 2.8f, 0f), "♪",
                        new Color(1f, 0.85f, 0.4f), 0.09f);
                }
            }

            labelTimer -= Time.unscaledDeltaTime;
            if (labelTimer > 0f || label == null) return;
            labelTimer = 0.5f;
            if (on) label.text = Current.displayName + "  " + NumberFormat.Clock(PerformLeft);
            else if (Ready) label.text = DisplayName + " - sẵn sàng";
            else label.text = DisplayName + " - hồi " + NumberFormat.Clock(CooldownLeft);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Interaction.Request(this);
        }

        public Vector3 InteractPoint => new Vector3(transform.position.x, Game.I.Street.StandY, 0f);

        public void Interact()
        {
            Game.I.UI.OpenStagePanel(this);
        }
    }
}
