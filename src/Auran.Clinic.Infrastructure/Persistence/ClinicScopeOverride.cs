namespace Auran.Clinic.Infrastructure.Persistence;

public sealed class ClinicScopeOverride
{
    private Guid? _clinicId;

    public Guid? ClinicId => _clinicId;

    public IDisposable Enter(Guid clinicId)
    {
        if (clinicId == Guid.Empty)
            throw new ArgumentException("Clinic scope override requires a valid clinic id.", nameof(clinicId));

        var previous = _clinicId;
        _clinicId = clinicId;
        return new Lease(this, previous);
    }

    private sealed class Lease(ClinicScopeOverride owner, Guid? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            owner._clinicId = previous;
            _disposed = true;
        }
    }
}
