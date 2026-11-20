using DentalImageMaker.Main.Automation;

namespace DentalImageMaker.Main.Services;

public interface IGenerationService
{
    Task<GenerationResult> GenerateAsync(AiProvider provider, GenerationRequest request, IProgress<string> progress, CancellationToken ct);
}

/// <summary>
/// 한 번에 한 건씩만 보낸다. 같은 브라우저 화면을 두 작업이 동시에 만지면 서로 꼬이기 때문이다.
/// 브라우저 조작은 UI 스레드에서만 되므로 호출도 UI 스레드에서 한다.
/// </summary>
public sealed class GenerationService(IBrowserSessionService browser) : IGenerationService
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<GenerationResult> GenerateAsync(AiProvider provider, GenerationRequest request, IProgress<string> progress, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var page = await browser.GetDriverAsync(provider);
            return await browser.GetAutomation(provider).GenerateAsync(page, request, progress, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenerationResult.Fail("오류: " + ex.Message);
        }
        finally
        {
            _gate.Release();
        }
    }
}
