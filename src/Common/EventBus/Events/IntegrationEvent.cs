namespace EventBus.Events
{
    public abstract class IntegrationEvent
    {
        public Guid Id { get; set; }
        public DateTime CreationDate { get; set; }

        protected IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
        }

        protected IntegrationEvent(Guid id, DateTime creationDate)
        {
            Id = id;
            CreationDate = creationDate;
        }
    }
}