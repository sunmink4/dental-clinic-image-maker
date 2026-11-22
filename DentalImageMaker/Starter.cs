using Velopack;

namespace DentalImageMaker;

public class Starter
{
    [STAThread]
    private static void Main(string[] args)
    {
        // 설치판이면 Velopack 이 설치·업데이트·제거 때 특수 인자로 이 exe 를 부른다. 맨 앞에서 처리하고 끝낸다.
        // 설치 없이 쓰는 단일 exe 에서는 아무 일도 하지 않는다.
        VelopackApp.Build().Run();
        _ = new App().Run();
    }
}
