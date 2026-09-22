using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enum 멤버 이름과 동일한 자식 UI 오브젝트를 바인딩하는 UI 기본 클래스입니다.
/// </summary>
public abstract class UI_Base : MonoBehaviour
{
    private readonly Dictionary<Type, UnityEngine.Object[]> objects = new();
    private bool isInitialized;
    private bool isInitializing;
    private HubCanvasController owner;

    public bool IsInitialized => isInitialized;
    protected HubCanvasController Owner => owner;

    /// <summary>
    /// UI를 최초 생성한 직후 호출하는 초기화 진입점입니다.
    /// 여러 번 호출되더라도 실제 초기화는 한 번만 실행됩니다.
    /// </summary>
    public void Init(HubCanvasController owner)
    {
        if (owner == null)
        {
            Debug.LogError($"[{GetType().Name}] owner 없이 UI를 초기화할 수 없습니다.", this);
            return;
        }

        if (isInitialized || isInitializing)
        {
            if (this.owner != owner)
            {
                Debug.LogError($"[{GetType().Name}] 이미 다른 owner로 초기화된 UI입니다.", this);
            }

            return;
        }

        this.owner = owner;
        isInitializing = true;

        try
        {
            OnInit();
            isInitialized = true;
        }
        finally
        {
            isInitializing = false;
        }
    }

    /// <summary>
    /// 파생 UI에서 필요한 바인딩과 이벤트 연결을 구현합니다.
    /// Awake나 Start에서 직접 호출하지 않습니다.
    /// </summary>
    protected abstract void OnInit();

    /// <summary>
    /// UI를 활성화하고 등장 연출이 끝날 때까지 대기합니다.
    /// </summary>
    public IEnumerator Show()
    {
        if (!isInitialized)
        {
            Debug.LogError($"[{GetType().Name}] Init(owner) 호출 전에 UI를 표시할 수 없습니다.", this);
            yield break;
        }

        gameObject.SetActive(true);
        yield return OnShow();
    }

    /// <summary>
    /// 퇴장 연출이 끝난 뒤 UI를 비활성화합니다.
    /// </summary>
    public IEnumerator Hide()
    {
        yield return OnHide();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 파생 UI의 등장 연출을 구현합니다.
    /// 연출이 없다면 재정의하지 않아도 됩니다.
    /// </summary>
    protected virtual IEnumerator OnShow()
    {
        yield break;
    }

    /// <summary>
    /// 파생 UI의 퇴장 연출을 구현합니다.
    /// 연출이 없다면 재정의하지 않아도 됩니다.
    /// </summary>
    protected virtual IEnumerator OnHide()
    {
        yield break;
    }

    /// <summary>
    /// Enum의 각 멤버 이름과 동일한 자식 오브젝트 또는 컴포넌트를 바인딩합니다.
    /// </summary>
    protected void Bind<T>(Type enumType) where T : UnityEngine.Object
    {
        if (enumType == null || !enumType.IsEnum)
        {
            Debug.LogError($"[{GetType().Name}] Bind에는 Enum 타입이 필요합니다.", this);
            return;
        }

        string[] names = Enum.GetNames(enumType);
        UnityEngine.Object[] boundObjects = new UnityEngine.Object[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            Transform child = FindChild(names[i]);
            if (child == null)
            {
                Debug.LogWarning($"[{GetType().Name}] 자식 UI '{names[i]}'를 찾을 수 없습니다.", this);
                continue;
            }

            if (typeof(T) == typeof(GameObject))
            {
                boundObjects[i] = child.gameObject;
            }
            else if (typeof(Component).IsAssignableFrom(typeof(T)))
            {
                boundObjects[i] = child.GetComponent(typeof(T));
            }

            if (boundObjects[i] == null)
            {
                Debug.LogWarning(
                    $"[{GetType().Name}] '{names[i]}'에서 {typeof(T).Name} 컴포넌트를 찾을 수 없습니다.",
                    child);
            }
        }

        // 바인딩을 다시 구성하는 경우에도 최신 값으로 안전하게 교체합니다.
        objects[typeof(T)] = boundObjects;
    }

    /// <summary>
    /// Enum의 정수 값을 사용해 바인딩한 UI를 가져옵니다.
    /// </summary>
    protected T GetUI<T>(int index) where T : UnityEngine.Object
    {
        if (!objects.TryGetValue(typeof(T), out UnityEngine.Object[] boundObjects))
        {
            Debug.LogWarning($"[{GetType().Name}] {typeof(T).Name} 타입이 아직 Bind되지 않았습니다.", this);
            return null;
        }

        if (index < 0 || index >= boundObjects.Length)
        {
            Debug.LogError($"[{GetType().Name}] UI 인덱스 {index}가 범위를 벗어났습니다.", this);
            return null;
        }

        return boundObjects[index] as T;
    }

    
    public Text GetText(int index) => GetUI<Text>(index);
    public Button GetButton(int index) => GetUI<Button>(index);
    public Image GetImage(int index) => GetUI<Image>(index);
    public GameObject GetGameObject(int index) => GetUI<GameObject>(index);
    public TextMeshProUGUI GetTextMeshPro(int index) => GetUI<TextMeshProUGUI>(index);

    private Transform FindChild(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }

}
