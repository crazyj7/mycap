# MyCap - 화면 캡처 응용 프로그램

MyCap은 Windows 환경에서 동작하는 사용자 친화적인 화면 캡처 응용 프로그램입니다. 전체 화면, 특정 영역, 또는 특정 창의 스크린샷을 쉽게 캡처하고 저장할 수 있습니다.

## 주요 기능

- **다양한 캡처 모드**:
  - 전체 화면 캡처
  - 특정 영역 캡처
  - 특정 창 캡처

- **이미지 관리**:
  - 캡처된 이미지 저장
  - 클립보드에 이미지 복사
  - 자동 저장 기능

- **사용자 설정**:
  - 저장 위치 설정
  - 단축키 설정
  - 사용자 기본 설정

## 시스템 요구 사항

- Windows 운영 체제
- .NET 8.0 런타임
- 최소 4GB RAM 권장

## 설치 방법

1. 최신 릴리스에서 설치 파일을 다운로드하세요.
2. 다운로드한 파일을 실행하고 설치 과정을 따르세요.
3. 설치가 완료되면 바탕화면이나 시작 메뉴에서 MyCap을 실행할 수 있습니다.

## 개발 환경 설정

```bash
# 리포지토리 클론
git clone https://github.com/yourusername/mycap.git

# 프로젝트 디렉토리로 이동
cd mycap

# 프로젝트 빌드
dotnet build

# 애플리케이션 실행
dotnet run
```

## 기술 스택

- C# / WPF (.NET 8.0)
- Windows Forms (부분적 사용)
- System.Drawing.Common

## 프로젝트 구조

- `MainWindow.xaml/cs`: 메인 애플리케이션 창 및 로직
- `Services/`: 캡처, 저장 등의 핵심 기능을 제공하는 서비스 클래스
- `Windows/`: 추가 창 및 대화 상자 컴포넌트

## 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다. 자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.

## 기여 방법

1. 이 저장소를 포크합니다.
2. 새로운 기능 브랜치를 생성합니다 (`git checkout -b feature/amazing-feature`).
3. 변경 사항을 커밋합니다 (`git commit -m 'Add some amazing feature'`).
4. 브랜치에 푸시합니다 (`git push origin feature/amazing-feature`).
5. Pull Request를 생성합니다.

## 연락처

프로젝트 관리자 - [이메일 주소](mailto:your-email@example.com)

프로젝트 링크: [https://github.com/yourusername/mycap](https://github.com/yourusername/mycap) 