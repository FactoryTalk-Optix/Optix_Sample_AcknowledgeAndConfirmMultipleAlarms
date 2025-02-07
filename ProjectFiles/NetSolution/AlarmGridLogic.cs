#region Using directives
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.HMIProject;
using System;
using FTOptix.EventLogger;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using System.Collections.Generic;
using System.Linq;
#endregion

public class AlarmGridLogic : BaseNetLogic
{
    #region Start and Stop
    public override void Start()
    {
        alarmsDataGridModel = Owner.Get<DataGrid>("AlarmsDataGrid").GetVariable("Model");

        var currentSession = LogicObject.Context.Sessions.CurrentSessionInfo;
        actualLanguageVariable = currentSession.SessionObject.Get<IUAVariable>("ActualLanguage");
        actualLanguageVariable.VariableChange += OnSessionActualLanguageChange;
    }

    public override void Stop()
    {
        actualLanguageVariable.VariableChange -= OnSessionActualLanguageChange;
    }
    #endregion

    #region Language change

    /// <summary>
    /// Handles the event when the actual language of the session changes.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void OnSessionActualLanguageChange(object sender, VariableChangeEventArgs e)
    {
        var dynamicLink = alarmsDataGridModel.GetVariable("DynamicLink");
        if (dynamicLink == null)
            return;

        // Restart the data bind on the data grid model variable to refresh data
        string dynamicLinkValue = dynamicLink.Value;
        dynamicLink.Value = string.Empty;
        dynamicLink.Value = dynamicLinkValue;
    }

    #endregion

    #region Alarms operations

    #region Methods to be called by the AlarmGrid
    [ExportMethod]
    public void AcknowledgeAllAlarmsWithDialog()
    {
        var myDialog = Project.Current.Get<DialogType>("UI/Templates/AckConfirmMessage");
        var dialogInstance = UICommands.OpenDialog(Owner, myDialog);
        dialogInstance.GetVariable("Action").Value = "Acknowledge";
        dialogInstance.GetVariable("MethodName").Value = "AckAllAlarmsWithMessage";
        dialogInstance.GetVariable("AlarmGridLogic").Value = LogicObject.NodeId;
    }

    [ExportMethod]
    public void ConfirmAllAlarmsWithDialog()
    {
        var myDialog = Project.Current.Get<DialogType>("UI/Templates/AckConfirmMessage");
        var dialogInstance = UICommands.OpenDialog(Owner, myDialog);
        dialogInstance.GetVariable("Action").Value = "Confirm";
        dialogInstance.GetVariable("MethodName").Value = "ConfirmAllAlarmsWithMessage";
        dialogInstance.GetVariable("AlarmGridLogic").Value = LogicObject.NodeId;
    }

    [ExportMethod]
    public void AcknowledgeSelectedAlarmsWithDialog()
    {
        var myDialog = Project.Current.Get<DialogType>("UI/Templates/AckConfirmMessage");
        var dialogInstance = UICommands.OpenDialog(Owner, myDialog);
        dialogInstance.GetVariable("Action").Value = "Acknowledge";
        dialogInstance.GetVariable("MethodName").Value = "AckSelectedAlarmsWithMessage";
        dialogInstance.GetVariable("AlarmGridLogic").Value = LogicObject.NodeId;
    }

    [ExportMethod]
    public void ConfirmSelectedAlarmsWithDialog()
    {
        var myDialog = Project.Current.Get<DialogType>("UI/Templates/AckConfirmMessage");
        var dialogInstance = UICommands.OpenDialog(Owner, myDialog);
        dialogInstance.GetVariable("Action").Value = "Confirm";
        dialogInstance.GetVariable("MethodName").Value = "ConfirmSelectedAlarmsWithMessage";
        dialogInstance.GetVariable("AlarmGridLogic").Value = LogicObject.NodeId;
    }
    #endregion

    #region Methods to be called by the DialogBox
    /// <summary>
    /// Acknowledges the selected alarms with a specified message.
    /// </summary>
    /// <param name="arguments">The object containing the message to be used in the alarm action.</param>
    [ExportMethod]
    public void AckSelectedAlarmsWithMessage(object[] arguments)
    {
        var ackMessage = (LocalizedText) arguments[0];
        var alarmsList = GetSelectedAlarms();
        ProcessAlarms(ackMessage, alarmsList, (alarm, message) => alarm.Acknowledge(message));
    }

    /// <summary>
    /// Confirms the selected alarms with a specified message.
    /// </summary>
    /// <param name="arguments">The object containing the message to be used in the alarm action.</param>
    [ExportMethod]
    public void ConfirmSelectedAlarmsWithMessage(object[] arguments)
    {
        var confirmMessage = (LocalizedText) arguments[0];
        var alarmsList = GetSelectedAlarms();
        ProcessAlarms(confirmMessage, alarmsList, (alarm, message) => alarm.Confirm(message));
    }

    /// <summary>
    /// Acknowledges all alarms with a specified message.
    /// </summary>
    /// <param name="arguments">The object containing the message to be used in the alarm action.</param>
    [ExportMethod]
    public void AckAllAlarmsWithMessage(object[] arguments)
    {
        var ackMessage = (LocalizedText) arguments[0];
        var alarmsList = GetAllAlarms();
        if (alarmsList.Count == 0)
        {
            Log.Warning("No alarms to acknowledge");
            return;
        }
        ProcessAlarms(ackMessage, alarmsList, (alarm, message) => alarm.Acknowledge(message));
    }

    /// <summary>
    /// Confirms all alarms with a specified message.
    /// </summary>
    /// <param name="arguments">The object containing the message to be used in the alarm action.</param>
    [ExportMethod]
    public void ConfirmAllAlarmsWithMessage(object[] arguments)
    {
        var confirmMessage = (LocalizedText) arguments[0];
        var alarmsList = GetAllAlarms();
        if (alarmsList.Count == 0)
        {
            Log.Warning("No alarms to confirm");
            return;
        }
        ProcessAlarms(confirmMessage, alarmsList, (alarm, message) => alarm.Confirm(message));
    }
    #endregion

    #region Private methods

    /// <summary>
    /// Processes the selected alarms with a specified action.
    /// </summary>
    /// <param name="message">The message to be used for the action.</param>
    /// <param name="alarmAction">The action to be performed on the alarms.</param>
    private static void ProcessAlarms(LocalizedText message, List<AlarmController> alarms, Action<AlarmController, LocalizedText> alarmAction)
    {
        foreach (var alarm in alarms)
        {
            alarmAction(alarm, message);
        }
    }

    /// <summary>
    /// Get the selected alarms from the data grid.
    /// </summary>
    private List<AlarmController> GetSelectedAlarms()
    {
        var dataGrid = Owner.Get<DataGrid>("AlarmsDataGrid");
        var selectedItemsNodes = dataGrid.GetOptionalVariableValue("UISelectedItems") ?? throw new System.ArgumentException("UISelectedItems variable not found in AlarmsDataGrid");
        var selectedItemsArray = (NodeId[]) selectedItemsNodes.Value;
        if (selectedItemsArray == null || selectedItemsArray.Length == 0)
        {
            throw new System.ArgumentException("No alarms selected");
        }
        var selectedAlarms = new List<AlarmController>();
        foreach (var nodeId in selectedItemsArray)
        {
            var alarm = GetAlarmFromRetainedAlarm(nodeId) ?? throw new System.ArgumentException("Alarm not found");
            selectedAlarms.Add(alarm);
        }
        return selectedAlarms;
    }

    /// <summary>
    /// Get the list of all alarms currently active.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    private List<AlarmController> GetAllAlarms()
    {
        // Get the alarms server object
        var retainedAlarmsObject = LogicObject.Context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);
        // Get the object containing the actual list of alarms
        var localizedAlarmsObject = retainedAlarmsObject.GetVariable("LocalizedAlarms");
        var localizedAlarmsNodeId = (NodeId) localizedAlarmsObject.Value;
        IUANode localizedAlarmsContainer = null;
        if (localizedAlarmsNodeId?.IsEmpty == false)
            localizedAlarmsContainer = LogicObject.Context.GetNode(localizedAlarmsNodeId);
        if (localizedAlarmsContainer == null)
        {
            Log.Error("AlarmsObserverLogic", "LocalizedAlarms node not found");
            throw new ArgumentException("Cannot find the LocalizedAlarms node");
        }

        // Cast all children to a List of AlarmController
        return localizedAlarmsContainer.Children
            .Select(child => GetAlarmFromRetainedAlarm(child.NodeId))
            .Where(alarm => alarm != null)
            .ToList();
    }


    /// <summary>
    /// Retrieves the <see cref="AlarmController"/> associated with the given retained alarm ID.
    /// </summary>
    /// <param name="retainedAlarmId">The <see cref="NodeId"/> of the retained alarm.</param>
    /// <returns>The <see cref="AlarmController"/> associated with the retained alarm.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the alarm is not found.</exception>
    private static AlarmController GetAlarmFromRetainedAlarm(NodeId retainedAlarmId)
    {
        // Get the alarm controller from the retained alarm
        var retainedAlarm = InformationModel.Get(retainedAlarmId);
        // Get the alarm controller from the retained alarm
        return InformationModel.Get<AlarmController>(retainedAlarm.GetVariable("ConditionId").Value) ?? throw new System.ArgumentException("Alarm not found");
    }

    #endregion

    #endregion

    private IUAVariable alarmsDataGridModel;
    private IUAVariable actualLanguageVariable;
}
