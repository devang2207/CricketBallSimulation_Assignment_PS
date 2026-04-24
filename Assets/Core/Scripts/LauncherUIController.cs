using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ProjectileLauncher))]
public class LauncherUIController : MonoBehaviour
{
    [Header("Mode Buttons")]
    public Button swingButton;
    public Button spinButton;

    [Header("Arm Buttons")]
    public Button leftArmButton;
    public Button rightArmButton;

    [Header("Sliders")]
    public Slider swingSlider;
    public Slider spinSlider;

    ProjectileLauncher launcher;

    readonly Color activeColor   = new Color(0.2f, 0.7f, 0.3f);
    readonly Color inactiveColor = new Color(0.35f, 0.35f, 0.35f);

    void Start()
    {
        launcher = GetComponent<ProjectileLauncher>();

        swingSlider.minValue = -20f;
        swingSlider.maxValue =  20f;
        swingSlider.value    = launcher.curve;

        spinSlider.minValue = -10f;
        spinSlider.maxValue =  10f;
        spinSlider.value    = launcher.spin;

        swingButton.onClick.AddListener(() => SetMode(ProjectileLauncher.DeviationMode.Swing));
        spinButton.onClick.AddListener(() => SetMode(ProjectileLauncher.DeviationMode.Spin));

        leftArmButton.onClick.AddListener(() => SetArm(false));
        rightArmButton.onClick.AddListener(() => SetArm(true));

        swingSlider.onValueChanged.AddListener(v => launcher.curve = v);
        spinSlider.onValueChanged.AddListener(v => launcher.spin = v);

        SetMode(launcher.mode);
        SetArm(launcher.useRightArm);
    }

    void SetMode(ProjectileLauncher.DeviationMode mode)
    {
        launcher.mode = mode;

        bool isSwing = mode == ProjectileLauncher.DeviationMode.Swing;

        swingSlider.interactable = isSwing;
        spinSlider.interactable  = !isSwing;

        swingButton.GetComponent<Image>().color = isSwing ? activeColor : inactiveColor;
        spinButton.GetComponent<Image>().color  = isSwing ? inactiveColor : activeColor;
    }

    void SetArm(bool rightArm)
    {
        launcher.useRightArm = rightArm;

        rightArmButton.GetComponent<Image>().color = rightArm ? activeColor : inactiveColor;
        leftArmButton.GetComponent<Image>().color  = rightArm ? inactiveColor : activeColor;
    }
}
