using System;
using System.Net.Http;
using Observatory.Framework.Files;
using Observatory.Framework.Files.Journal;

namespace Observatory.Framework.Interfaces
{
    /// <summary>
    /// <para>Base plugin interface containing methods common to both notifiers and workers.</para>
    /// <para>Note: Not intended to be implemented on its own and will not define a functional plugin. Use IObservatoryWorker, IObservatoryNotifier, or both, as appropriate.</para>
    /// </summary>
    public interface IObservatoryPluginAsync : IObservatoryPlugin
    {
        /// <summary>
        /// <para>This method will be called on startup by Observatory Core when a plugin is first loaded.</para>
        /// <para>Passes the Core interface to the plugin.</para>
        /// </summary>
        /// <param name="observatoryCore">Object implementing Observatory Core's main interface. A reference to this object should be maintained by the plugin for communication back to Core.</param>
        public Task LoadAsync(IObservatoryCoreAsync observatoryCore);

        public Task UnloadAsync();
    }

    /// <summary>
    /// <para>Interface for worker plugins which process journal data to update their UI or send notifications.</para>
    /// <para>Work required on plugin startup — for example object instantiation — can be done in the constructor or Load() method.<br/>
    /// Be aware that saved settings will not be available until Load() is called.</para>
    /// </summary>
    public interface IObservatoryWorkerAsync : IObservatoryPluginAsync
    {
        /// <summary>
        /// Method called when new journal data is processed. Most work done by worker plugins will occur here.
        /// </summary>
        /// <typeparam name="TJournal">Specific type of journal entry being received.</typeparam>
        /// <param name="journal"><para>Elite Dangerous journal event, deserialized into a .NET object.</para>
        /// <para>Unhandled json values within a journal entry type will be contained in member property:<br/>Dictionary&lt;string, object&gt; AdditionalProperties.</para>
        /// <para>Unhandled journal event types will be type JournalBase with all values contained in AdditionalProperties.</para></param>
        public Task JournalEventAsync<TJournal>(TJournal journal) where TJournal : JournalBase;

        /// <summary>
        /// Method called when status.json content is updated.<br/>
        /// Can be omitted for plugins which do not use this data.
        /// </summary>
        /// <param name="status">Player status.json content, deserialized into a .NET object.</param>
        public Task StatusChangeAsync(Status status);


        /// <summary>
        /// Called when the LogMonitor changes state. Useful for suppressing output in certain situations
        /// such as batch reads (ie. "Read all") or responding to other state transitions.
        /// </summary>
        public Task LogMonitorStateChangedAsync(LogMonitorStateChangedEventArgs eventArgs);
    }

    /// <summary>
    /// <para>Interface for notifier plugins which receive notification events from other plugins for any purpose.</para>
    /// <para>Work required on plugin startup — for example object instantiation — can be done in the constructor or Load() method.<br/>
    /// Be aware that saved settings will not be available until Load() is called.</para>
    /// </summary>
    public interface IObservatoryNotifierAsync : IObservatoryPluginAsync
    {
        /// <summary>
        /// Method called when other plugins send notification events to Observatory Core.
        /// </summary>
        /// <param name="notificationEventArgs">Details of the notification as sent from the originating worker plugin.</param>
        public Task OnNotificationEventAsync(NotificationArgs notificationEventArgs);
    }

    public interface IInbuiltNotifierAsync : IObservatoryNotifierAsync
    {
        public NotificationRendering Filter { get; }

        public Task OnNotificationEventAsync(Guid id, NotificationArgs notificationEventArgs);

        public Task OnNotificationCancelledAsync(Guid id);
    }


    /// <summary>
    /// Interface passed by Observatory Core to plugins. Primarily used for sending notifications and UI updates back to Core.
    /// </summary>
    public interface IObservatoryCoreAsync : IObservatoryCore
    {
        public IEnumerable<IObservatoryPlugin> Initialize();

        /// <summary>
        /// Send a notification out to all native notifiers and any plugins implementing IObservatoryNotifier.
        /// </summary>
        /// <param name="title">Title text for notification.</param>
        /// <param name="detail">Detail/body text for notificaiton.</param>
        /// <returns>Guid associated with the notification during its lifetime. Used as an argument with CancelNotification and UpdateNotification.</returns>
        public Task<Guid> SendNotificationAsync(string title, string detail);

        /// <summary>
        /// Send a notification with arguments out to all native notifiers and any plugins implementing IObservatoryNotifier.
        /// </summary>
        /// <param name="notificationEventArgs">NotificationArgs object specifying notification content and behaviour.</param>
        /// <returns>Guid associated with the notification during its lifetime. Used as an argument with CancelNotification and UpdateNotification.</returns>
        public Task<Guid> SendNotificationAsync(NotificationArgs notificationEventArgs);
        
        /// <summary>
        /// Cancel or close an active notification.
        /// </summary>
        /// <param name="notificationId">Guid of notification to be cancelled.</param>
        public Task CancelNotificationAsync(Guid notificationId);

        /// <summary>
        /// Update an active notification with a new set of NotificationsArgs. Timeout values are reset and begin counting again from zero if specified.
        /// </summary>
        /// <param name="notificationId">Guid of notification to be updated.</param>
        /// <param name="notificationEventArgs">NotificationArgs object specifying updated notification content and behaviour.</param>
        public Task UpdateNotificationAsync(Guid notificationId, NotificationArgs notificationEventArgs);

        /// <summary>
        /// Requests current Elite Dangerous status.json content.
        /// </summary>
        /// <returns>Status object reflecting current Elite Dangerous player status.</returns>
        public Task<Status> GetStatusAsync();

        public T GetService<T>();
    }
}
