#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.EventLogger;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.Alarm;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.Core;
#endregion

public class AckConfirmMessageLogic : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        alarmsNetLogicPointer = Owner.GetVariable("AlarmGridLogic");
        alarmsNetLogicPointer.VariableChange += OnAlarmGridLogicChange;
    }

    private void OnAlarmGridLogicChange(object sender, VariableChangeEventArgs e)
    {
        alarmsNetLogic = GetNetLogicObject();
        if (alarmsNetLogic == null)
        {
            Log.Error("AlarmGridLogic object not found");
        }
        else
        {
            Log.Verbose1("AlarmGridLogic object updated");
        }
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        alarmsNetLogicPointer.VariableChange -= OnAlarmGridLogicChange;
    }

    [ExportMethod]
    public void PerformAction()
    {
        // Get the action name to be executed
        string action = Owner.GetVariable("MethodName").Value;
        // Get the comment to be added to the alarm
        var comment = Owner.Get<TextBox>("Comment").LocalizedText;
        // Create the object arguments to be passed to the method
        object[] arguments = new object[] { comment };
        alarmsNetLogic.ExecuteMethod(action, [arguments]);
        (Owner as Dialog)?.Close();
    }

    private IUAObject GetNetLogicObject()
    {
        return InformationModel.GetObject(Owner.GetVariable("AlarmGridLogic").Value);
    }

    private IUAObject alarmsNetLogic;
    private IUAVariable alarmsNetLogicPointer;
}
