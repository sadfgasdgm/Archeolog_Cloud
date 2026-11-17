
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Управление")]
    [SerializeField] VirtualJoystick joystick;
    [SerializeField] Transform cam;
    [SerializeField] float walkSpeed = 2f;
    [SerializeField] float fastSpeed = 3f;
    [SerializeField] float stepHeight = 0.25f;
    [SerializeField] float turnSpeed = 10f;

    public bool Fast { get; private set; }

    bool canMove = true;

    CharacterController cc;
    float verticalVel;
    float defaultCenterY;

    // Все Renderer персонажа включая корневой объект
    Renderer[] playerRenderers;
    bool[] originalRendererStates;

    public bool CanMove
    {
        get => canMove;
        set
        {
            canMove = value;

            if (!value)
                Fast = false;

            // Отключаем физическое движение во время раскопок
            if (cc != null)
                cc.enabled = value;

            // Скрываем или возвращаем всю модель персонажа
            SetPlayerVisible(value);
        }
    }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cc.stepOffset = stepHeight;
        defaultCenterY = cc.center.y;

        // Получаем все рендереры включая дочерние и корневой
        playerRenderers = GetComponentsInChildren<Renderer>(true);

        originalRendererStates = new bool[playerRenderers.Length];

        for (int i = 0; i < playerRenderers.Length; i++)
        {
            originalRendererStates[i] = playerRenderers[i].enabled;
        }
    }

    void SetPlayerVisible(bool visible)
    {
        if (playerRenderers == null)
            return;

        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] == null)
                continue;

            // При раскопках отключаем все Renderer.
            // После раскопок восстанавливаем исходное состояние.
            playerRenderers[i].enabled =
                visible && originalRendererStates[i];
        }
    }

    public void ToggleFast()
    {
        if (!canMove)
            return;

        Fast = !Fast;
    }

    void Update()
    {
        if (!cc.enabled || cam == null)
            return;

        // Фиксируем центр капсулы
        Vector3 currentCenter = cc.center;
        currentCenter.y = defaultCenterY;
        cc.center = currentCenter;

        Vector2 input = joystick != null
            ? joystick.Direction
            : Vector2.zero;

        if (input == Vector2.zero)
        {
            input = new Vector2(
                Input.GetAxis("Horizontal"),
                Input.GetAxis("Vertical")
            );
        }

        // Движение относительно камеры
        Vector3 fwd = Vector3.ProjectOnPlane(
            cam.forward, Vector3.up
        ).normalized;

        Vector3 right = Vector3.ProjectOnPlane(
            cam.right, Vector3.up
        ).normalized;

        Vector3 dir = canMove
            ? (fwd * input.y + right * input.x)
            : Vector3.zero;

        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        float speed = Fast ? fastSpeed : walkSpeed;

        // Гравитация
        if (cc.isGrounded)
        {
            verticalVel = -2f;
        }
        else
        {
            verticalVel -= 9.81f * Time.deltaTime;
        }

        Vector3 moveVector =
            (dir * speed + Vector3.up * verticalVel)
            * Time.deltaTime;

        cc.Move(moveVector);

        // Поворот персонажа
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(dir);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }
    }
}
