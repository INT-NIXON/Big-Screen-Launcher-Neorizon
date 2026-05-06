using BSLN.Core.Domain;

namespace BSLN.Core.Application.Abstractions;

public interface IControllerInputSource
{
    event EventHandler<SemanticInputAction>? ActionReceived;
    event EventHandler<InputDeviceFamily>? InputFamilyChanged;

    void Start();
    void Stop();
}
