## Docs 인덱스

### 목적
- **단일 사실 원천(Single Source of Truth)**: 기획/아키텍처/개발 계획을 코드와 함께 버전 관리합니다.
- **작업 기준 합의**: 개발 중 반복 질문을 줄이고, 공통 레퍼런스를 제공합니다.

### 사용 방법
- 변경이 생기면 관련 문서를 **같이 수정**하고 커밋합니다.
- 큰 결정(중요 정책/규칙 변경)은 `Architecture.md` 또는 `Conventions.md`에 반영하고, 요약을 `README.md`에 링크합니다.
- 릴리스/스프린트 단위 변경 요약은 `DevelopmentPlan.md`와 커밋 메시지에 기록합니다.

### 문서 목록
- **프로젝트 개요 및 핵심 루프**: [GameDesign.md](./GameDesign.md)
- **아키텍처/모듈 관계/흐름**: [Architecture.md](./Architecture.md)
- **개발 계획/마일스톤/우선순위**: [DevelopmentPlan.md](./DevelopmentPlan.md)
- **코딩 컨벤션/리뷰 원칙**: [Conventions.md](./Conventions.md)
- **용어집/약어/도메인 정의**: [Glossary.md](./Glossary.md)

### 업데이트 원칙
- 문서는 **코드 변경 이전/동시에 갱신**합니다.
- 파일 간 **중복 기술 금지**: 원문은 한 곳에, 나머지는 링크/요약으로 연결합니다.
- 결정 사항은 **근거와 대안**을 1~2줄로 남깁니다.

### 체크리스트
- [ ] 새 기능/규칙 → 관련 문서에 반영했는가?
- [ ] 용어/데이터 구조 변경 → `Glossary.md`/`Architecture.md` 업데이트 했는가?
- [ ] 마일스톤/우선순위 변경 → `DevelopmentPlan.md` 수정했는가?


