namespace TagEkyc.Domain;

public sealed record ActorReference
{
    public ActorReference(string actorId, string actorCategory)
    {
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new ArgumentException("Actor id is required.", nameof(actorId));
        }

        if (string.IsNullOrWhiteSpace(actorCategory))
        {
            throw new ArgumentException("Actor category is required.", nameof(actorCategory));
        }

        ActorId = actorId;
        ActorCategory = actorCategory;
    }

    public string ActorId { get; }
    public string ActorCategory { get; }
}
