using System;

namespace Naninovel
{
    /// <summary>
    /// Arguments associated with the <see cref="ICustomVariableManager.OnVariableUpdated"/> event. 
    /// </summary>
    public class CustomVariableUpdatedArgs : EventArgs
    {
        /// <summary>
        /// Name of the updated variable.
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// New value of the updated variable.
        /// Null when variable is deleted.
        /// </summary>
        public readonly CustomVariableValue? Value;
        /// <summary>
        /// Value the variable had before the update.
        /// Null when variable was just created.
        /// </summary>
        public readonly CustomVariableValue? InitialValue;

        public CustomVariableUpdatedArgs (string name, CustomVariableValue? value, CustomVariableValue? initialValue)
        {
            Name = name;
            Value = value;
            InitialValue = initialValue;
        }
    }
}
