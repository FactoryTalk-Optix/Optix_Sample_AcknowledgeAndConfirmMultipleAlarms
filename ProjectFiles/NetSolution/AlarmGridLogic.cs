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
#endregion

public class AlarmGridLogic : BaseNetLogic
{
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

    /// <summary>
    /// Acknowledges the selected alarms with a specified message.
    /// </summary>
    /// <param name="ackMessage">The acknowledgment message.</param>
    [ExportMethod]
    public void AckAlarmsWithMessage(LocalizedText ackMessage)
    {
        ProcessAlarms(ackMessage, (alarm, message) => alarm.Acknowledge(message));
    }

    /// <summary>
    /// Confirms the selected alarms with a specified message.
    /// </summary>
    /// <param name="confirmMessage">The confirmation message.</param>
    [ExportMethod]
    public void ConfirmAlarmsWithMessage(LocalizedText confirmMessage)
    {
        ProcessAlarms(confirmMessage, (alarm, message) => alarm.Confirm(message));
    }

    #region Private methods

    /// <summary>
    /// Processes the selected alarms with a specified action.
    /// </summary>
    /// <param name="message">The message to be used for the action.</param>
    /// <param name="alarmAction">The action to be performed on the alarms.</param>
    private void ProcessAlarms(LocalizedText message, Action<AlarmController, LocalizedText> alarmAction)
    {
        var dataGrid = Owner.Get<DataGrid>("AlarmsDataGrid");

        if (dataGrid.GetVariable("AllowMultiSelection").Value)
        {
            // Multi selection
            var selectedItemsNodes = dataGrid.GetOptionalVariableValue("UISelectedItems") ?? throw new System.ArgumentException("UISelectedItems variable not found in AlarmsDataGrid");

            var selectedItemsArray = (NodeId[]) selectedItemsNodes.Value;
            if (selectedItemsArray == null || selectedItemsArray.Length == 0)
            {
                throw new System.ArgumentException("No alarms selected");
            }

            // Process each selected alarm
            foreach (var nodeId in selectedItemsArray)
            {
                var alarm = GetAlarmFromRetainedAlarm(nodeId) ?? throw new System.ArgumentException("Alarm not found");
                alarmAction(alarm, message);
            }
        }
        else
        {
            // Single selection
            var alarm = GetAlarmFromRetainedAlarm(dataGrid.UISelectedItem) ?? throw new System.ArgumentException("Alarm not found");
            alarmAction(alarm, message);
        }
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
