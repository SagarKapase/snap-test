namespace snap_test.Helpers
{
    /// <summary>
    /// Anything stored in an in-memory seed store with an integer primary key.
    /// Required by <see cref="snap_test.Controllers.CrudControllerBase{T}"/>.
    /// </summary>
    public interface IEntity
    {
        int Id { get; set; }
    }
}
