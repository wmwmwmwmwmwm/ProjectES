using System.Collections.Generic;
using Naninovel.Expression;

namespace Naninovel.Commands
{
    /// <summary>
    /// Assigns result of a [script expression](/guide/script-expressions) to a [custom variable](/guide/custom-variables).
    /// </summary>
    /// <remarks>
    /// If a variable with the provided name doesn't exist, it will be automatically created.
    /// <br/><br/>
    /// Specify multiple set expressions by separating them with `;`. The expressions will be executed in sequence in the order of declaration.
    /// <br/><br/>
    /// In case variable name starts with `t_` it's considered a reference to a value stored in 'Script' [managed text](/guide/managed-text) document. 
    /// Such variables can't be assigned and are intended for referencing localizable text values.
    /// </remarks>
    [CommandAlias("set")]
    public class SetCustomVariable : Command, Command.IForceWait
    {
        /// <summary>
        /// Assignment expression.
        /// <br/><br/>
        /// The expression should be in the following format: `var=expression`, where `var` is the name of the custom 
        /// variable to assign and `expression` is a [script expression](/guide/script-expressions), the result of which should be assigned to the variable.
        /// <br/><br/>
        /// It's possible to use increment and decrement unary operators (`@set foo++`, `@set foo--`) and compound assignment (`@set foo+=10`, `@set foo-=3`, `@set foo*=0.1`, `@set foo/=2`).
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, AssignmentContext]
        public StringParameter Expression;

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            var vars = Engine.GetService<ICustomVariableManager>();
            var asses = new List<Assignment>();

            ExpressionEvaluator.ParseAssignments(Expression, asses, LogErrorMessage);
            foreach (var ass in asses)
                AssignEvaluated(ass.Variable, ExpressionEvaluator.Evaluate(ass.Expression, LogErrorMessage), vars);

            if (ShouldSaveGlobalState(asses))
                await Engine.GetService<IStateManager>().SaveGlobalAsync();
        }

        protected virtual void AssignEvaluated (string var, IOperand result, ICustomVariableManager vars)
        {
            if (result is String str) vars.SetVariableValue(var, new CustomVariableValue(str.Value));
            else if (result is Boolean boo) vars.SetVariableValue(var, new CustomVariableValue(boo.Value));
            else vars.SetVariableValue(var, new CustomVariableValue((float)((Numeric)result).Value));
        }

        protected virtual bool ShouldSaveGlobalState (IReadOnlyList<Assignment> asses)
        {
            foreach (var eval in asses)
                if (CustomVariablesConfiguration.HasGlobalPrefix(eval.Variable))
                    return true;
            return false;
        }

        protected virtual void LogErrorMessage (string desc = null)
        {
            Err($"Failed to evaluate assignment expression '{Expression}'. {desc ?? string.Empty}");
        }
    }
}
