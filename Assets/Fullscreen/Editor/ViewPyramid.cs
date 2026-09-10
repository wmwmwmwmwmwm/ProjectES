using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using HostView = UnityEngine.ScriptableObject;
using View = UnityEngine.ScriptableObject;
using ContainerWindow = UnityEngine.ScriptableObject;

namespace FullscreenEditor {
    /// <summary>Represents the pyramid containing all the elements that make up a window.</summary>
    [Serializable]
    public struct ViewPyramid {

        /// <summary>The actual window, may be null if the pyramid was created from a view or container.</summary>
        public EditorWindow Window {
            get {
                if (!m_window && m_windowInstanceID != EntityId.None)
                    m_window = (EditorWindow)EditorUtility.EntityIdToObject(m_windowInstanceID);
                return m_window;
            }
            set {
                m_window = value;
                m_windowInstanceID = m_window ? m_window.GetEntityId() : EntityId.None;
            }
        }

        /// <summary>View that controls how the window (and child view) are drawn.</summary>
        public View View {
            get {
                if (!m_view && m_viewInstanceID != EntityId.None)
                    m_view = (View)EditorUtility.EntityIdToObject(m_viewInstanceID);
                return m_view;
            }
            set {
                value.EnsureOfType(Types.View);
                m_view = value;
                m_viewInstanceID = m_view ? m_view.GetEntityId() : EntityId.None;
            }
        }

        /// <summary>The native window.</summary>
        public ContainerWindow Container {
            get {
                if (!m_container && m_containerInstanceID != EntityId.None)
                    m_container = (ContainerWindow)EditorUtility.EntityIdToObject(m_containerInstanceID);
                return m_container;
            }
            set {
                value.EnsureOfType(Types.ContainerWindow);
                m_container = value;
                m_containerInstanceID = m_container ? m_container.GetEntityId() : EntityId.None;
            }
        }

        [SerializeField] private EditorWindow m_window;
        [SerializeField] private View m_view;
        [SerializeField] private ContainerWindow m_container;

        [SerializeField] private EntityId m_windowInstanceID;
        [SerializeField] private EntityId m_viewInstanceID;
        [SerializeField] private EntityId m_containerInstanceID;

        /// <summary>Create a new instance and automatically assigns the window, view and container.</summary>
        public ViewPyramid(ScriptableObject viewOrWindow) {

            if (!viewOrWindow) {
                m_window = null;
                m_view = null;
                m_container = null;
            } else if (viewOrWindow.IsOfType(typeof(EditorWindow))) {
                m_window = viewOrWindow as EditorWindow;
                m_view = m_window.GetFieldValue<View>("m_Parent");
                m_container = m_view.GetPropertyValue<ContainerWindow>("window");
            } else if (viewOrWindow.IsOfType(Types.View)) {
                m_window = null;
                m_view = viewOrWindow;
                m_container = m_view.GetPropertyValue<ContainerWindow>("window");
            } else if (viewOrWindow.IsOfType(Types.ContainerWindow)) {
                m_window = null;
                m_view = viewOrWindow.GetPropertyValue<ContainerWindow>("rootView");
                m_container = viewOrWindow;
            } else {
                throw new ArgumentException("Param must be of type EditorWindow, View or ContainerWindow", "viewOrWindow");
            }

            if (!m_window && m_view && m_view.IsOfType(Types.HostView))
                m_window = m_view.GetPropertyValue<EditorWindow>("actualView");

            m_windowInstanceID = m_window ? m_window.GetEntityId() : EntityId.None;
            m_viewInstanceID = m_view ? m_view.GetEntityId() : EntityId.None;
            m_containerInstanceID = m_container ? m_container.GetEntityId() : EntityId.None;

        }

    }
}
