using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Collections;
using System.Text;
using UnityEngine.UI;
using System;
using System.Reflection;

public class Console : MonoBehaviour
{
    private static Console s_Inst;

    public static Console Inst
    {
        get
        {
            if (s_Inst == null)
            {
                Initialize();
            }
            return s_Inst;
        }
    }

    private class WindowInfo
    {
        public List<KeyValuePair<string, UnityAction>> Buttons;

        public WindowInfo()
        {
            Buttons = new List<KeyValuePair<string, UnityAction>>();
        }
    }

    public Texture2D Tex1;
    public Texture2D Tex2;

    private bool m_IsOpen;
    private bool m_IsEnterBtnCollapse;
    private bool m_IsWindowCollapse;
    private Rect m_RootRect;
    private Rect m_EnterBtnRect;
    private Rect m_CmdRect;
    private Vector2 m_CmdHelp;
    private float m_RootHeight;
    private string m_Current;
    private string m_CmdInput;
    private EventSystem m_EventSytem;
    private Dictionary<string, WindowInfo> m_Menus;
    private Dictionary<string, UnityAction<string>> m_Commands;

    private bool m_Inited;
    private GUIStyle m_EnterBtnStyle;
    private GUIStyle m_EnterBtnCollapseStyle;
    private GUIStyle m_MenuBtnStyle;
    private GUIStyle m_WndStyle;
    private GUIStyle m_SmlBtnStyle;
    private GUIStyle m_HrzBarStyle;
    private GUIStyle m_VtcBarStyle;
    private GUIStyle m_MenuArea;
    private GUIStyle m_ContentArea;
    private GUIStyle m_CmBtnStyle;
    private GUIStyle m_CmdInputStyle;
    private GUIStyle m_CmdTipStyle;
    private GUIStyle m_CmdHelpStyle;
    private GUIStyle m_ScrollBarStyle;
    private GUIStyle m_ScrollBarStyle2;
    private Vector2 m_MenuPos;
    private Vector2 m_ContentsPos;
    private float m_FitWidth;
    private const string Default = nameof(Default);

    private int m_MaxCmd;
    private Node m_CmdNode;
    private Dictionary<string, CommandDescInfo> m_CmdDesc;

    private void Awake()
    {
        m_IsWindowCollapse = true;
        m_IsEnterBtnCollapse = false;
        m_MaxCmd = 15;
        m_Menus = new Dictionary<string, WindowInfo>();
        m_Commands = new Dictionary<string, UnityAction<string>>();
        m_CmdDesc = new Dictionary<string, CommandDescInfo>();
        m_CmdNode = new Node(string.Empty);
#if LUA
        StartCoroutine(StartUp());
#endif
    }
#if LUA
    private IEnumerator StartUp()
    {
        yield return new WaitForSeconds(1);
        CXLuaMgr.Instance.GetLuaEnv().DoString("require 'console/console';console.initialize()");
    }
#endif

    [RuntimeInitializeOnLoadMethod]
    public static void Initialize()
    {
        if (s_Inst != null)
            return;
        GameObject inst = new GameObject(nameof(Console));
        s_Inst = inst.AddComponent<Console>();
    }

    public void AddButton(string menu, string btn, UnityAction handler)
    {
        if (menu == null)
            menu = Default;
        if (m_Current == null)
            m_Current = menu;
        if (!m_Menus.TryGetValue(menu, out WindowInfo info))
        {
            info = new WindowInfo();
            m_Menus[menu] = info;
        }
        info.Buttons.Add(new KeyValuePair<string, UnityAction>(btn, handler));
    }

    public void AddCommand(string cmd, UnityAction<string> handler)
    {
        m_Commands[cmd] = handler;
        m_CmdNode.Add(cmd);
    }

    public void AddCommandDesc(string cmd, string param, string desc)
    {
        m_CmdDesc[cmd] = new CommandDescInfo(
            string.IsNullOrEmpty(param) ? string.Empty : $"  {param.TrimStart(' ')}",
            string.IsNullOrEmpty(desc) ? string.Empty : $"  {desc.TrimStart(' ')}");
    }

    public void ExecuteCommand(string fullParam)
    {
        InnerRunCommand(fullParam);
    }

    public void Close()
    {
        StartCoroutine(InnerClose());
    }

    private IEnumerator InnerClose()
    {
        yield return new WaitForEndOfFrame();
        m_IsOpen = false;
    }

    private void InnerInitStyle()
    {
        if (m_Inited)
            return;
        m_Inited = true;
        m_FitWidth = Screen.width / 720f;

        #region Styles
        Texture2D tex1 = new Texture2D(1, 1);
        tex1.SetPixel(0, 0, new Color(12 / 255f, 28 / 255f, 47 / 255f, 0.8f));
        tex1.Apply();

        Texture2D tex2 = new Texture2D(1, 1);
        tex2.SetPixel(0, 0, new Color(12 / 255f, 28 / 255f, 47 / 255f, 1));
        tex2.Apply();

        m_EnterBtnStyle = new GUIStyle(GUI.skin.button);
        m_EnterBtnStyle.fixedHeight = InnerFitWidth(40);
        m_EnterBtnStyle.fixedWidth = InnerFitWidth(130);
        m_EnterBtnStyle.fontSize = (int)InnerFitWidth(30);
        m_EnterBtnStyle.normal.background = tex1;
        m_EnterBtnStyle.active.background = tex2;
        m_EnterBtnStyle.hover.background = tex2;
        m_EnterBtnStyle.onNormal.background = tex1;
        m_EnterBtnStyle.margin = new RectOffset();

        m_EnterBtnCollapseStyle = new GUIStyle(m_EnterBtnStyle);
        m_EnterBtnCollapseStyle.fixedHeight = InnerFitWidth(40);
        m_EnterBtnCollapseStyle.fixedWidth = InnerFitWidth(25);
        m_EnterBtnCollapseStyle.fontSize = (int)InnerFitWidth(30);
        m_EnterBtnCollapseStyle.margin = new RectOffset();

        m_EnterBtnRect = new Rect(0, 0, m_EnterBtnStyle.fixedWidth + m_EnterBtnStyle.fixedWidth, m_EnterBtnStyle.fixedHeight);

        m_CmdInputStyle = new GUIStyle(GUI.skin.textField);
        m_CmdInputStyle.fixedHeight = (int)InnerFitWidth(40);
        m_CmdInputStyle.fixedWidth = InnerFitWidth(550);
        m_CmdInputStyle.fontSize = (int)InnerFitWidth(30);

        m_CmdTipStyle = new GUIStyle(GUI.skin.textField);
        m_CmdTipStyle.fontSize = (int)InnerFitWidth(30);
        m_CmdTipStyle.richText = true;

        m_CmBtnStyle = new GUIStyle(GUI.skin.button);
        m_CmBtnStyle.fixedHeight = InnerFitWidth(40);
        m_CmBtnStyle.fontSize = (int)InnerFitWidth(30);
        m_CmBtnStyle.margin.bottom = (int)InnerFitWidth(10);

        m_MenuBtnStyle = new GUIStyle(GUI.skin.button);
        m_MenuBtnStyle.fixedHeight = InnerFitWidth(40);
        m_MenuBtnStyle.fontSize = (int)InnerFitWidth(30);
        m_MenuBtnStyle.margin.bottom = (int)InnerFitWidth(10);
        m_MenuBtnStyle.normal.background = GUI.skin.horizontalScrollbarThumb.normal.background;
        m_MenuBtnStyle.hover.background = GUI.skin.horizontalScrollbarThumb.hover.background;
        m_MenuBtnStyle.active.background = GUI.skin.horizontalScrollbarThumb.active.background;
        m_MenuBtnStyle.focused.background = GUI.skin.horizontalScrollbarThumb.focused.background;

        m_SmlBtnStyle = new GUIStyle(GUI.skin.button);
        m_SmlBtnStyle.fixedHeight = InnerFitWidth(40);
        m_SmlBtnStyle.fixedWidth = InnerFitWidth(40);
        m_SmlBtnStyle.fontSize = (int)InnerFitWidth(30);
        m_SmlBtnStyle.alignment = TextAnchor.MiddleCenter;

        m_WndStyle = new GUIStyle(GUI.skin.window);
        m_WndStyle.stretchWidth = true;
        m_WndStyle.stretchHeight = true;
        m_WndStyle.fixedWidth = InnerFitWidth(720);
        m_WndStyle.fixedHeight = 0;
        m_WndStyle.normal.background = tex1;
        m_WndStyle.onNormal.background = tex1;

        m_CmdHelpStyle = new GUIStyle(GUI.skin.window);
        m_CmdHelpStyle.fixedWidth = InnerFitWidth(720);
        m_CmdHelpStyle.fixedHeight = 0;
        m_CmdHelpStyle.normal.background = tex1;
        m_CmdHelpStyle.onNormal.background = tex1;

        m_HrzBarStyle = new GUIStyle(GUI.skin.horizontalScrollbar);
        m_HrzBarStyle.fixedHeight = InnerFitWidth(70);

        m_VtcBarStyle = new GUIStyle(GUI.skin.verticalScrollbar);
        m_VtcBarStyle.fixedWidth = InnerFitWidth(70);

        m_MenuArea = new GUIStyle(GUI.skin.window);
        m_MenuArea.fixedWidth = InnerFitWidth(220);
        m_MenuArea.stretchWidth = true;
        m_MenuArea.fixedHeight = InnerFitWidth(300);

        m_ContentArea = new GUIStyle(GUI.skin.window);
        m_ContentArea.stretchWidth = true;
        m_ContentArea.fixedWidth = 0;
        m_ContentArea.fixedHeight = InnerFitWidth(300);

        m_ScrollBarStyle = new GUIStyle(GUI.skin.verticalScrollbar);
        m_ScrollBarStyle.fixedWidth = 40;
        GUI.skin.verticalScrollbarThumb.fixedWidth = 0;
        GUI.skin.verticalScrollbarThumb.stretchWidth = true;

        m_ScrollBarStyle2 = new GUIStyle(GUI.skin.horizontalScrollbar);
        m_ScrollBarStyle2.fixedHeight = 40;
        GUI.skin.horizontalScrollbarThumb.fixedHeight = 0;
        GUI.skin.horizontalScrollbarThumb.stretchHeight = true;
        #endregion

        m_CmdRect = new Rect();

    }

    private float InnerFitWidth(float width)
    {
        return width * m_FitWidth;
    }

    private void OnGUI()
    {
        InnerInitStyle();
        InternalCheckInGUI();
        if (m_IsOpen)
        {
            m_RootRect = default;
            m_RootRect = GUILayout.Window(0, m_RootRect, InternalDrawRootWindow, string.Empty, m_WndStyle);
            if (m_RootRect.height > 0)
                m_RootHeight = m_RootRect.height;

            if (!string.IsNullOrEmpty(m_CmdInput))
            {
                m_CmdRect.y = m_RootHeight;
                m_CmdRect = GUILayout.Window(1, m_CmdRect, InternalDrawCmdWindow, string.Empty, m_CmdHelpStyle);
            }
        }
        else
        {
            GUILayout.BeginHorizontal();
            if (!m_IsEnterBtnCollapse && GUILayout.Button(InnerToGreen("Console"), m_EnterBtnStyle))
                m_IsOpen = true;
            if (GUILayout.Button(InnerToBlue(m_IsEnterBtnCollapse ? ">" : "<"), m_EnterBtnCollapseStyle))
                m_IsEnterBtnCollapse = !m_IsEnterBtnCollapse;
            GUILayout.EndHorizontal();
        }
    }

    private void InternalCheckInGUI()
    {
        if (m_EventSytem == null)
        {
            m_EventSytem = EventSystem.current;
        }
        else
        {
            Vector3 touchPos = Input.mousePosition;
            touchPos.y = Screen.height - touchPos.y;
            bool enable = m_IsOpen ? !m_RootRect.Contains(touchPos) && !m_CmdRect.Contains(touchPos) :
                !m_EnterBtnRect.Contains(touchPos);
            m_EventSytem.enabled = enable;
            if (!enable)
            {
                Input.ResetInputAxes();
            }
        }
    }

    private void InnerCheckSpecialInput()
    {
        int length = m_CmdInput.Length;
        if (length <= 0)
            return;
        char ch = m_CmdInput[length - 1];
        switch (ch)
        {
            case '\n':
                m_CmdInput = m_CmdInput.Remove(length - 1);
                InnerRunCommand();
                break;

            case '`':
                m_CmdInput = m_CmdInput.Remove(length - 1);
                CommandShowInfo[] cmds = InnerGetCurrentCmds();
                m_CmdInput = cmds[0].Cmd;

                TextEditor edt = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                edt.text = m_CmdInput;
                edt.MoveLineEnd();
                break;
        }
    }

    private void InternalDrawCmdWindow(int windowId)
    {
        InnerCheckSpecialInput();

        CommandShowInfo[] cmds = InnerGetCurrentCmds();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < cmds.Length; i++)
        {
            CommandShowInfo cmd = cmds[i];
            switch (cmd.Type)
            {
                case CommandShowInfoType.Match:
                    sb.Append(InnerToGreen(cmd.Cmd));
                    if (m_CmdDesc.TryGetValue(cmd.Cmd, out CommandDescInfo info))
                    {
                        sb.Append(InnerToOrange(info.Param));
                        sb.Append(InnerToCyan(info.Desc));
                    }
                    break;

                case CommandShowInfoType.WillMatch:
                    sb.Append(cmd.Cmd);
                    if (m_CmdDesc.TryGetValue(cmd.Cmd, out info))
                    {
                        sb.Append(InnerToOrange(info.Param));
                        sb.Append(InnerToCyan(info.Desc));
                    }
                    break;

                case CommandShowInfoType.NotMatch:
                    sb.Append(InnerToRed(cmd.Cmd));
                    break;
            }

            if (i < cmds.Length - 1)
                sb.Append('\n');
        }
        m_CmdRect.height = InnerFitWidth(45 + Mathf.Min(m_MaxCmd, cmds.Length) * 35);
        m_CmdHelp = GUILayout.BeginScrollView(m_CmdHelp, false, false, m_ScrollBarStyle2, m_ScrollBarStyle);
        GUILayout.TextField(sb.ToString(), m_CmdTipStyle);
        GUILayout.EndScrollView();
    }

    private CommandShowInfo[] InnerGetCurrentCmds()
    {
        string cmd = m_CmdInput.Trim(' ').Split(' ')[0];
        return cmd == "?" ? m_CmdNode.GetValues() : m_CmdNode.GetValues(cmd);
    }

    private void InternalDrawRootWindow(int windowId)
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(InnerToOrange(m_IsWindowCollapse ? ">" : "v"), m_SmlBtnStyle))
            m_IsWindowCollapse = !m_IsWindowCollapse;
        if (GUILayout.Button(InnerToRed("x"), m_SmlBtnStyle))
            m_IsOpen = false;
        GUILayout.EndHorizontal();

        if (!m_IsWindowCollapse)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(m_MenuArea);
            m_MenuPos = GUILayout.BeginScrollView(m_MenuPos, false, false, null, m_ScrollBarStyle);
            foreach (string menu in m_Menus.Keys)
            {
                if (menu == m_Current)
                {
                    if (GUILayout.Button(InnerToOrange(menu), m_MenuBtnStyle))
                        m_Current = menu;
                }
                else
                {
                    if (GUILayout.Button(menu, m_CmBtnStyle))
                        m_Current = menu;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(m_ContentArea);
            m_ContentsPos = GUILayout.BeginScrollView(m_ContentsPos, false, false, null, m_ScrollBarStyle);
            if (m_Current != null)
            {
                if (m_Menus.TryGetValue(m_Current, out WindowInfo info))
                {
                    foreach (var item in info.Buttons)
                    {
                        if (GUILayout.Button(item.Key, m_CmBtnStyle))
                        {
                            item.Value();
                        }
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        m_CmdInput = GUILayout.TextArea(m_CmdInput, m_CmdInputStyle);
        if (GUILayout.Button(InnerToBlue("Run"), m_CmBtnStyle))
            InnerRunCommand();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void InnerRunCommand()
    {
        InnerRunCommand(m_CmdInput);
    }

    private void InnerRunCommand(string fullParam)
    {
        string[] cmds = fullParam.Trim(' ').Split(' ');
        string cmd = cmds[0];
        if (m_Commands.TryGetValue(cmd, out UnityAction<string> handler))
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 1; i < cmds.Length; i++)
            {
                sb.Append(cmds[i]);
                sb.Append(" ");
            }
            handler(sb.ToString());
        }
        else
        {
            Debug.Log("Command dont exist");
        }
    }

    private static string InnerToGreen(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        if (text.StartsWith("<color"))
            return text;
        return $"<color=#00FF00>{text}</color>";
    }

    private static string InnerToBlue(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        if (text.StartsWith("<color"))
            return text;
        return $"<color=#4dc6ff>{text}</color>";
    }

    private static string InnerToRed(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        if (text.StartsWith("<color"))
            return text;
        return $"<color=#FF0000>{text}</color>";
    }

    private static string InnerToOrange(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        if (text.StartsWith("<color"))
            return text;
        return $"<color=#FFA500>{text}</color>";
    }

    private static string InnerToCyan(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        if (text.StartsWith("<color"))
            return text;
        return $"<color=#00FFFF>{text}</color>";
    }

    public class Node
    {
        public string Value { get; private set; }

        public bool IsCmd { get; private set; }

        public Dictionary<string, Node> Children { get; private set; }

        public Node(string value)
        {
            Value = value;
            Children = new Dictionary<string, Node>();
        }

        public void Add(string fullValue)
        {
            if (fullValue.Length > 0)
            {
                string cur = fullValue[0].ToString();
                if (!Children.TryGetValue(cur, out Node node))
                {
                    node = new Node(cur);
                    if (fullValue.Length <= 1)
                    {
                        node.IsCmd = true;
                    }

                    Children.Add(cur, node);
                }

                if (fullValue.Length > 1)
                {
                    StringBuilder sb = new StringBuilder(fullValue.Substring(1));
                    node.Add(sb.ToString());
                }
            }
        }

        public CommandShowInfo[] GetValues(string prefix)
        {
            List<CommandShowInfo> values = new List<CommandShowInfo>();
            Node node = InnerRecursiveFindNode(string.Empty, prefix);
            if (node != null)
            {
                if (node.Children.Count <= 0 || node.IsCmd)
                {
                    values.Add(new CommandShowInfo(prefix, CommandShowInfoType.Match));
                }
                if (node.Children.Count > 0)
                {
                    foreach (Node child in node.Children.Values)
                    {
                        CommandShowInfo[] childValues = child.GetValues();
                        foreach (CommandShowInfo value in childValues)
                        {
                            values.Add(new CommandShowInfo(prefix + value.Cmd, value.Type));
                        }
                    }
                }
            }
            else
            {
                values.Add(new CommandShowInfo(prefix, CommandShowInfoType.NotMatch));
            }

            return values.ToArray();
        }

        private Node InnerRecursiveFindNode(string cur, string target)
        {
            cur += Value;
            if (cur == target)
                return this;
            foreach (Node node in Children.Values)
            {
                Node next = node.InnerRecursiveFindNode(cur, target);
                if (next != null)
                    return next;
            }
            return null;
        }

        public CommandShowInfo[] GetValues()
        {
            List<CommandShowInfo> values = new List<CommandShowInfo>();
            InnerRecursiveValue(string.Empty, values);
            return values.ToArray();
        }

        private void InnerRecursiveValue(string cur, List<CommandShowInfo> result)
        {
            cur += Value;
            if (Children.Count <= 0 || IsCmd)
            {
                result.Add(new CommandShowInfo(cur, CommandShowInfoType.WillMatch));
            }

            foreach (Node node in Children.Values)
            {
                node.InnerRecursiveValue(cur, result);
            }
        }
    }

    public enum CommandShowInfoType
    {
        Match,
        WillMatch,
        NotMatch
    }

    public struct CommandShowInfo
    {
        public string Cmd;
        public CommandShowInfoType Type;

        public CommandShowInfo(string cmd, CommandShowInfoType type)
        {
            Cmd = cmd;
            Type = type;
        }
    }

    public struct CommandDescInfo
    {
        public string Param;
        public string Desc;

        public CommandDescInfo(string param, string desc)
        {
            Param = param;
            Desc = desc;
        }
    }
}