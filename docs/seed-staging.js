// Run against staging MongoDB:
// mongosh "your-staging-connection-string" docs/seed-staging.js

const db = db.getSiblingDB("jobvault-staging");

// Clear existing data
db.job_applications.deleteMany({});
db.notifications.deleteMany({});
db.pending_jobs.deleteMany({});

print("Cleared existing data.");

// ── Job Applications ──

const now = new Date();
const daysAgo = (n) => new Date(now.getTime() - n * 86400000);

db.job_applications.insertMany([
  {
    _id: ObjectId(),
    companyName: "NovaTech GmbH",
    jobTitle: "Senior Backend Developer",
    location: "Berlin, Germany",
    jobUrl: "https://novatech.example.com/careers/senior-backend",
    workMode: "Hybrid",
    employmentType: "Full-time",
    salaryMin: 70000,
    salaryMax: 85000,
    currency: "EUR",
    salaryPeriod: "Annual",
    matchScore: 88,
    recommendation: "Strong Match",
    status: "Ready to Apply",
    stage: "Applied",
    applied: true,
    appliedDate: daysAgo(5),
    createdAt: daysAgo(8),
    updatedAt: daysAgo(5),
    jdSource: "LinkedIn",
    headline: "Senior Backend Developer with .NET and Cloud Experience",
    summary: "Experienced backend developer specializing in .NET, event-driven architectures, and cloud-native systems.",
    skills: [
      { label: "Backend", value: ".NET 9, C#, ASP.NET Core, REST APIs, Clean Architecture" },
      { label: "Message Broker", value: "RabbitMQ, Event-Driven Architecture" },
      { label: "Database", value: "MongoDB, PostgreSQL, Entity Framework" },
      { label: "Cloud", value: "Azure Functions, Azure Service Bus, Docker" },
      { label: "Frontend", value: "Vue.js, TypeScript" },
      { label: "CI/CD", value: "GitHub Actions, Docker Compose" }
    ],
    roles: [
      {
        id: "calvergy",
        bullets: [
          "Architected event-driven .NET microservices processing 50K+ daily shipment events via Azure Service Bus",
          "Reduced API response latency by 40% through async pipeline redesign and MongoDB query optimization",
          "Built real-time tracking dashboard serving 200+ concurrent logistics operators"
        ]
      }
    ],
    recipient: "Hiring Team",
    coverLetterParagraphs: [
      "I am writing to express my interest in the Senior Backend Developer position at NovaTech GmbH.",
      "With extensive experience in .NET ecosystem and event-driven architectures, I bring a strong foundation in building scalable backend systems.",
      "At my previous role, I architected microservices processing 50K+ daily events, which directly aligns with your team's technical direction.",
      "I look forward to discussing how my background in .NET, RabbitMQ, and cloud-native development can contribute to NovaTech's engineering goals."
    ],
    strengths: ["Strong .NET experience", "Event-driven architecture expertise", "Cloud-native background"],
    gaps: ["No Kubernetes experience mentioned"],
    commitUrl: "https://github.com/example/vault/commit/abc123",
    interviews: [
      { id: 1, date: "2026-06-22", type: "Phone Screen", notes: "30-min call with HR, discussed salary expectations", outcome: "Passed" },
      { id: 2, date: "2026-06-28", type: "Technical", notes: "Live coding session with team lead", outcome: "Pending" }
    ],
    notes: [
      { id: 1, category: "Research", content: "Company focused on logistics SaaS, 150 employees, Series B funded", stage: "Applied", pinned: true, createdAt: daysAgo(7), updatedAt: daysAgo(7) }
    ],
    salary: { advertised: "70-85K EUR", target: "80K EUR", discussed: "", offered: "" },
    recruiter: { name: "Sarah Mueller", email: "s.mueller@novatech.example.com", linkedin: "" },
    source: "LinkedIn",
    isHistorical: false
  },
  {
    _id: ObjectId(),
    companyName: "BlueShift Labs",
    jobTitle: "Full Stack Developer",
    location: "Frankfurt am Main, Germany",
    jobUrl: "https://blueshift.example.com/jobs/fullstack",
    workMode: "Remote",
    employmentType: "Full-time",
    salaryMin: 60000,
    salaryMax: 75000,
    currency: "EUR",
    salaryPeriod: "Annual",
    matchScore: 72,
    recommendation: "Good Match",
    status: "Ready to Apply",
    stage: "Ready to Apply",
    applied: false,
    createdAt: daysAgo(3),
    updatedAt: daysAgo(3),
    jdSource: "StepStone",
    headline: "Full Stack Developer — Vue.js & .NET Core",
    summary: "Full stack developer with strong Vue.js frontend skills and .NET backend experience.",
    skills: [
      { label: "Frontend", value: "Vue 3, TypeScript, Pinia, Tailwind CSS" },
      { label: "Backend", value: ".NET 8, ASP.NET Core, REST APIs" },
      { label: "Database", value: "PostgreSQL, MongoDB" },
      { label: "DevOps", value: "Docker, CI/CD" }
    ],
    roles: [
      {
        id: "senior_baris",
        bullets: [
          "Built Vue 3 SPA with TypeScript, Pinia state management, and real-time SSE notifications",
          "Developed RESTful APIs with ASP.NET Core following Clean Architecture principles",
          "Implemented CI/CD pipelines with GitHub Actions for automated testing and deployment"
        ]
      }
    ],
    recipient: "Engineering Team",
    coverLetterParagraphs: [
      "I am excited to apply for the Full Stack Developer role at BlueShift Labs.",
      "My experience spans both Vue.js frontend development and .NET backend systems, giving me the versatility your team is looking for.",
      "I recently built a full-stack application with Vue 3, TypeScript, and .NET 9 that includes real-time notifications, PWA support, and a complete CI/CD pipeline.",
      "I would welcome the opportunity to bring this full-stack expertise to BlueShift Labs."
    ],
    strengths: ["Vue 3 + TypeScript proficiency", "Full stack versatility", "CI/CD experience"],
    gaps: ["PostgreSQL preferred over MongoDB", "No React experience"],
    commitUrl: "https://github.com/example/vault/commit/def456",
    interviews: [],
    notes: [],
    source: "StepStone",
    isHistorical: false
  },
  {
    _id: ObjectId(),
    companyName: "Acme Technologies",
    jobTitle: "Backend Engineer",
    location: "Munich, Germany",
    jobUrl: "https://acme.example.com/careers/backend",
    workMode: "On-site",
    employmentType: "Full-time",
    salaryMin: 65000,
    salaryMax: 80000,
    currency: "EUR",
    salaryPeriod: "Annual",
    matchScore: 65,
    recommendation: "Moderate Match",
    status: "Ready to Apply",
    stage: "Applied",
    applied: true,
    appliedDate: daysAgo(12),
    createdAt: daysAgo(14),
    updatedAt: daysAgo(10),
    jdSource: "Company Website",
    headline: "Backend Engineer — Distributed Systems",
    summary: "Backend engineer with experience in distributed systems and message-driven architectures.",
    skills: [
      { label: "Backend", value: "C#, .NET, Go (preferred)" },
      { label: "Message Broker", value: "Kafka, RabbitMQ" },
      { label: "Database", value: "PostgreSQL, Redis" },
      { label: "Cloud", value: "AWS, Terraform" }
    ],
    roles: [
      {
        id: "developer_baris",
        bullets: [
          "Designed message-driven processing pipelines using RabbitMQ with dead-letter queues and retry strategies",
          "Implemented Clean Architecture patterns ensuring maintainable and testable codebases"
        ]
      }
    ],
    recipient: "Hiring Manager",
    coverLetterParagraphs: [
      "I am writing regarding the Backend Engineer position at Acme Technologies.",
      "My background in .NET and event-driven architectures aligns well with your distributed systems focus.",
      "While my primary experience is in .NET rather than Go, my architectural thinking and message-driven design patterns transfer directly.",
      "I am eager to contribute to Acme's engineering challenges and grow into your Go-based stack."
    ],
    strengths: ["Message-driven architecture", "Clean Architecture"],
    gaps: ["Go experience required", "AWS preferred over Azure", "On-site only — relocation needed"],
    commitUrl: "https://github.com/example/vault/commit/ghi789",
    interviews: [
      { id: 1, date: "2026-06-18", type: "Phone Screen", notes: "Brief intro call, discussed relocation timeline", outcome: "Passed" }
    ],
    notes: [
      { id: 1, category: "General", content: "Relocation package available, team of 8 backend engineers", stage: "Applied", pinned: false, createdAt: daysAgo(12), updatedAt: daysAgo(12) }
    ],
    salary: { advertised: "65-80K EUR", target: "75K EUR", discussed: "75K discussed in screening", offered: "" },
    recruiter: { name: "Max Fischer", email: "max.f@acme.example.com", linkedin: "https://linkedin.com/in/maxfischer" },
    source: "Company Website",
    isHistorical: false
  },
  {
    _id: ObjectId(),
    companyName: "Quantum Digital",
    jobTitle: "Software Developer (.NET)",
    location: "Stuttgart, Germany",
    jobUrl: "https://quantum.example.com/jobs/dotnet",
    workMode: "Hybrid",
    employmentType: "Full-time",
    salaryMin: 55000,
    salaryMax: 70000,
    currency: "EUR",
    salaryPeriod: "Annual",
    matchScore: 91,
    recommendation: "Strong Match",
    status: "Ready to Apply",
    stage: "Interview",
    applied: true,
    appliedDate: daysAgo(18),
    createdAt: daysAgo(20),
    updatedAt: daysAgo(2),
    jdSource: "LinkedIn",
    headline: "Software Developer — .NET Core, Clean Architecture, Event-Driven",
    summary: "Software developer with deep .NET expertise, Clean Architecture, and event-driven design.",
    skills: [
      { label: "Backend", value: ".NET 9, C#, ASP.NET Core, Clean Architecture" },
      { label: "Message Broker", value: "RabbitMQ" },
      { label: "Database", value: "MongoDB, SQL Server" },
      { label: "Frontend", value: "Vue.js or Angular" },
      { label: "DevOps", value: "Docker, GitHub Actions" }
    ],
    roles: [
      {
        id: "calvergy",
        bullets: [
          "Architected event-driven .NET microservices processing 50K+ daily shipment events via Azure Service Bus",
          "Reduced API response latency by 40% through async pipeline redesign and MongoDB query optimization",
          "Built real-time tracking dashboard serving 200+ concurrent logistics operators"
        ]
      },
      {
        id: "senior_baris",
        bullets: [
          "Led migration from monolithic ASP.NET MVC to Clean Architecture, improving test coverage from 15% to 65%",
          "Mentored junior developers on SOLID principles and domain-driven design"
        ]
      }
    ],
    recipient: "Technical Lead",
    coverLetterParagraphs: [
      "I am thrilled to apply for the Software Developer position at Quantum Digital.",
      "Your requirements read like my career summary — .NET Core, Clean Architecture, RabbitMQ, and Vue.js are the technologies I work with daily.",
      "My recent work includes building a complete event-driven pipeline with RabbitMQ dead-letter queues, retry strategies, and real-time SSE notifications.",
      "I am confident I can contribute to Quantum Digital from day one and look forward to discussing the role further."
    ],
    strengths: ["Perfect stack alignment", "Clean Architecture expertise", "Event-driven design"],
    gaps: ["Angular experience limited"],
    commitUrl: "https://github.com/example/vault/commit/jkl012",
    interviews: [
      { id: 1, date: "2026-06-15", type: "Phone Screen", notes: "Great conversation with HR, team culture sounds excellent", outcome: "Passed" },
      { id: 2, date: "2026-06-20", type: "Technical", notes: "1hr technical interview — discussed Clean Architecture, RabbitMQ patterns", outcome: "Passed" },
      { id: 3, date: "2026-06-30", type: "On-site", notes: "Trial day scheduled with the team", outcome: "Pending" }
    ],
    notes: [
      { id: 1, category: "Research", content: "Hospitality tech company, 80 employees, profitable", stage: "Interview", pinned: true, createdAt: daysAgo(19), updatedAt: daysAgo(19) },
      { id: 2, category: "Preparation", content: "Review RabbitMQ topic exchange patterns, prepare Clean Architecture examples", stage: "Interview", pinned: true, createdAt: daysAgo(3), updatedAt: daysAgo(3) }
    ],
    salary: { advertised: "55-70K EUR", target: "65K EUR", discussed: "65K range confirmed", offered: "" },
    recruiter: { name: "Lisa Weber", email: "l.weber@quantum.example.com", linkedin: "" },
    source: "LinkedIn",
    isHistorical: false
  },
  {
    _id: ObjectId(),
    companyName: "CloudBridge Solutions",
    jobTitle: "Senior .NET Developer",
    location: "Hamburg, Germany",
    jobUrl: "https://cloudbridge.example.com/senior-net",
    workMode: "Remote",
    employmentType: "Full-time",
    salaryMin: 75000,
    salaryMax: 90000,
    currency: "EUR",
    salaryPeriod: "Annual",
    matchScore: 78,
    recommendation: "Good Match",
    status: "Ready to Apply",
    stage: "Applied",
    applied: true,
    appliedDate: daysAgo(7),
    createdAt: daysAgo(10),
    updatedAt: daysAgo(7),
    jdSource: "Indeed",
    headline: "Senior .NET Developer — Cloud-First Architecture",
    summary: "Senior developer building cloud-first .NET applications with Azure expertise.",
    skills: [
      { label: "Backend", value: ".NET 8/9, C#, ASP.NET Core" },
      { label: "Cloud", value: "Azure, Azure Functions, Service Bus" },
      { label: "Database", value: "CosmosDB, SQL Server" },
      { label: "DevOps", value: "Terraform, Azure DevOps, Docker" }
    ],
    roles: [
      {
        id: "calvergy",
        bullets: [
          "Built Azure Functions processing 50K+ daily events with Service Bus integration",
          "Designed multi-tenant API gateway handling 500 req/s with sub-50ms latency"
        ]
      }
    ],
    recipient: "Engineering Manager",
    coverLetterParagraphs: [
      "I am interested in the Senior .NET Developer role at CloudBridge Solutions.",
      "My experience with Azure-based event-driven systems directly maps to your cloud-first architecture.",
      "I have designed and deployed high-throughput .NET services processing tens of thousands of daily events with Azure Service Bus.",
      "I am excited about the possibility of bringing my cloud-native expertise to your remote engineering team."
    ],
    strengths: ["Azure experience", "High-throughput systems", "Remote work experience"],
    gaps: ["CosmosDB — used MongoDB instead", "Terraform experience limited"],
    commitUrl: "https://github.com/example/vault/commit/mno345",
    interviews: [],
    notes: [],
    source: "Indeed",
    isHistorical: false
  },
  {
    _id: ObjectId(),
    companyName: "DataForge AG",
    jobTitle: "Backend Developer",
    location: "Düsseldorf, Germany",
    jobUrl: "https://dataforge.example.com/backend-dev",
    workMode: "Hybrid",
    employmentType: "Full-time",
    salaryMin: 58000,
    salaryMax: 72000,
    currency: "EUR",
    salaryPeriod: "Annual",
    matchScore: 45,
    recommendation: "Weak Match",
    status: "Ready to Apply",
    stage: "Rejected",
    applied: true,
    appliedDate: daysAgo(25),
    createdAt: daysAgo(28),
    updatedAt: daysAgo(15),
    jdSource: "LinkedIn",
    headline: "Backend Developer — Java/Spring Boot",
    summary: "Backend developer with Java and Spring Boot focus.",
    skills: [
      { label: "Backend", value: "Java 17, Spring Boot, Hibernate" },
      { label: "Database", value: "PostgreSQL, Redis" },
      { label: "Cloud", value: "AWS, ECS, Lambda" }
    ],
    roles: [
      {
        id: "developer_baris",
        bullets: [
          "Designed RESTful APIs following Clean Architecture principles with comprehensive test coverage",
          "Implemented async processing pipelines with message broker integration"
        ]
      }
    ],
    recipient: "HR Department",
    coverLetterParagraphs: [
      "I am applying for the Backend Developer position at DataForge AG.",
      "While my primary stack is .NET rather than Java, the architectural patterns I use — Clean Architecture, event-driven design, CI/CD — are directly transferable.",
      "I am eager to expand my backend expertise into the Java/Spring ecosystem while bringing proven design principles.",
      "I would appreciate the chance to discuss how my backend architecture skills can benefit DataForge."
    ],
    strengths: ["Clean Architecture transferable", "Backend fundamentals strong"],
    gaps: ["Java/Spring Boot required — no professional experience", "AWS preferred over Azure"],
    commitUrl: "https://github.com/example/vault/commit/pqr678",
    interviews: [],
    notes: [
      { id: 1, category: "Rejection", content: "Rejected via email — looking for Java-first candidates", stage: "Rejected", pinned: false, createdAt: daysAgo(15), updatedAt: daysAgo(15) }
    ],
    source: "LinkedIn",
    isHistorical: false
  },
  {
    _id: ObjectId(),
    companyName: "FinEdge Technologies",
    jobTitle: "Software Engineer",
    location: "Frankfurt am Main, Germany",
    jobUrl: "https://finedge.example.com/careers/swe",
    workMode: "Hybrid",
    employmentType: "Full-time",
    salaryMin: 68000,
    salaryMax: 82000,
    currency: "EUR",
    salaryPeriod: "Annual",
    matchScore: 82,
    recommendation: "Strong Match",
    status: "Ready to Apply",
    stage: "Ready to Apply",
    applied: false,
    createdAt: daysAgo(1),
    updatedAt: daysAgo(1),
    jdSource: "LinkedIn",
    headline: "Software Engineer — .NET, Event-Driven, FinTech",
    summary: "Software engineer for fintech platform with .NET and event-driven architecture.",
    skills: [
      { label: "Backend", value: ".NET 8+, C#, ASP.NET Core" },
      { label: "Message Broker", value: "RabbitMQ, Kafka" },
      { label: "Database", value: "PostgreSQL, MongoDB" },
      { label: "Frontend", value: "React or Vue.js" },
      { label: "DevOps", value: "Docker, Kubernetes, GitHub Actions" }
    ],
    roles: [
      {
        id: "calvergy",
        bullets: [
          "Architected event-driven .NET microservices processing 50K+ daily shipment events",
          "Reduced API response latency by 40% through async pipeline redesign"
        ]
      }
    ],
    recipient: "Hiring Team",
    coverLetterParagraphs: [
      "I am excited to apply for the Software Engineer position at FinEdge Technologies.",
      "My background in .NET and event-driven architectures aligns closely with your fintech platform requirements.",
      "I have hands-on experience with RabbitMQ message pipelines, MongoDB, and Vue.js — all part of your tech stack.",
      "I look forward to exploring how my skills can contribute to FinEdge's engineering team."
    ],
    strengths: ["Event-driven architecture", ".NET expertise", "Vue.js experience"],
    gaps: ["No Kubernetes production experience", "FinTech domain is new"],
    commitUrl: "https://github.com/example/vault/commit/stu901",
    interviews: [],
    notes: [],
    source: "LinkedIn",
    isHistorical: false
  },
  {
    _id: ObjectId(),
    companyName: "GreenCode Systems",
    jobTitle: ".NET Developer",
    location: "Wiesbaden, Germany",
    jobUrl: "https://greencode.example.com/net-developer",
    workMode: "On-site",
    employmentType: "Full-time",
    matchScore: 56,
    recommendation: "Moderate Match",
    status: "Processing",
    stage: "Processing",
    applied: false,
    createdAt: daysAgo(0),
    updatedAt: daysAgo(0),
    jdSource: "StepStone",
    headline: ".NET Developer for Enterprise Applications",
    summary: "Developer for enterprise .NET applications.",
    skills: [
      { label: "Backend", value: ".NET Framework 4.8, WCF, SOAP" },
      { label: "Database", value: "SQL Server, SSRS" },
      { label: "Frontend", value: "Angular, jQuery" }
    ],
    roles: [],
    recipient: "HR",
    coverLetterParagraphs: [],
    strengths: [".NET background"],
    gaps: ["Legacy stack (.NET Framework, WCF, SOAP)", "Angular required"],
    interviews: [],
    notes: [],
    source: "StepStone",
    isHistorical: false
  },
  {
    _id: ObjectId(),
    companyName: "Meridian Software",
    jobTitle: "Lead Backend Developer",
    location: "Cologne, Germany",
    jobUrl: "https://meridian.example.com/lead-backend",
    workMode: "Hybrid",
    employmentType: "Full-time",
    salaryMin: 80000,
    salaryMax: 95000,
    currency: "EUR",
    salaryPeriod: "Annual",
    matchScore: 74,
    recommendation: "Good Match",
    status: "Ready to Apply",
    stage: "Offer",
    applied: true,
    appliedDate: daysAgo(30),
    createdAt: daysAgo(35),
    updatedAt: daysAgo(1),
    jdSource: "LinkedIn",
    headline: "Lead Backend Developer — .NET Microservices",
    summary: "Lead developer to drive backend architecture and mentor the team.",
    skills: [
      { label: "Backend", value: ".NET 8, C#, gRPC, REST" },
      { label: "Architecture", value: "Microservices, DDD, CQRS" },
      { label: "Database", value: "PostgreSQL, Redis, MongoDB" },
      { label: "Cloud", value: "Azure, Docker, Kubernetes" }
    ],
    roles: [
      {
        id: "senior_baris",
        bullets: [
          "Led migration from monolithic ASP.NET MVC to Clean Architecture",
          "Mentored junior developers on SOLID principles and domain-driven design",
          "Reduced deployment time from 2 hours to 15 minutes with CI/CD pipeline redesign"
        ]
      }
    ],
    recipient: "CTO",
    coverLetterParagraphs: [
      "I am applying for the Lead Backend Developer role at Meridian Software.",
      "My experience leading architecture migrations and mentoring development teams aligns with your leadership requirements.",
      "I have hands-on experience driving Clean Architecture adoption, implementing CI/CD pipelines, and building event-driven systems.",
      "I am ready to take on the technical leadership responsibilities this role demands."
    ],
    strengths: ["Architecture leadership", "Team mentoring", "CI/CD expertise"],
    gaps: ["gRPC — limited experience", "CQRS — theoretical, not production"],
    commitUrl: "https://github.com/example/vault/commit/vwx234",
    interviews: [
      { id: 1, date: "2026-06-05", type: "Phone Screen", notes: "Great call with CTO", outcome: "Passed" },
      { id: 2, date: "2026-06-12", type: "Technical", notes: "Architecture discussion, whiteboard session", outcome: "Passed" },
      { id: 3, date: "2026-06-19", type: "On-site", notes: "Full day with the team, pair programming session", outcome: "Passed" },
      { id: 4, date: "2026-06-25", type: "Offer Call", notes: "Verbal offer: 85K + benefits", outcome: "Pending" }
    ],
    notes: [
      { id: 1, category: "Offer", content: "85K base + 10% bonus, 30 days vacation, hybrid 2 days/week", stage: "Offer", pinned: true, createdAt: daysAgo(1), updatedAt: daysAgo(1) }
    ],
    salary: { advertised: "80-95K EUR", target: "85K EUR", discussed: "85K confirmed", offered: "85K + 10% bonus" },
    recruiter: { name: "Anna Schmidt", email: "a.schmidt@meridian.example.com", linkedin: "https://linkedin.com/in/annaschmidt" },
    source: "LinkedIn",
    isHistorical: false
  },
  {
    _id: ObjectId(),
    companyName: "UrbanTech Berlin",
    jobTitle: "Backend Developer",
    location: "Berlin, Germany",
    jobUrl: "https://urbantech.example.com/backend",
    workMode: "Remote",
    employmentType: "Contract",
    salaryMin: 500,
    salaryMax: 650,
    currency: "EUR",
    salaryPeriod: "Daily",
    matchScore: 68,
    recommendation: "Good Match",
    status: "Ready to Apply",
    stage: "Applied",
    applied: true,
    appliedDate: daysAgo(4),
    createdAt: daysAgo(6),
    updatedAt: daysAgo(4),
    jdSource: "Freelancermap",
    headline: "Backend Developer — .NET Contract (6 months)",
    summary: "Contract developer for API modernization project.",
    skills: [
      { label: "Backend", value: ".NET 8, ASP.NET Core, REST APIs" },
      { label: "Database", value: "PostgreSQL" },
      { label: "DevOps", value: "Docker, Azure DevOps" }
    ],
    roles: [
      {
        id: "developer_baris",
        bullets: [
          "Developed RESTful APIs with ASP.NET Core following Clean Architecture principles",
          "Implemented comprehensive integration tests reducing production bugs by 60%"
        ]
      }
    ],
    recipient: "Project Manager",
    coverLetterParagraphs: [
      "I am interested in the Backend Developer contract at UrbanTech Berlin.",
      "My .NET and API development experience makes me a strong candidate for your modernization project.",
      "I can deliver clean, well-tested APIs following established architectural patterns.",
      "I am available to start immediately and committed for the full 6-month engagement."
    ],
    strengths: ["API development", "Quick onboarding"],
    gaps: ["Contract role — prefer permanent", "Azure DevOps — used GitHub Actions"],
    commitUrl: "https://github.com/example/vault/commit/yza567",
    interviews: [],
    notes: [],
    source: "Freelancermap",
    isHistorical: false
  }
]);

print("Inserted 10 job applications.");

// ── Pending Jobs (Job Queue) ──

db.pending_jobs.insertMany([
  {
    _id: ObjectId(),
    url: "https://linkedin.com/jobs/view/3847261",
    status: "completed",
    createdAt: daysAgo(2),
    updatedAt: daysAgo(2)
  },
  {
    _id: ObjectId(),
    url: "https://stepstone.de/jobs/senior-net-developer-hamburg",
    status: "completed",
    createdAt: daysAgo(1),
    updatedAt: daysAgo(1)
  },
  {
    _id: ObjectId(),
    url: "https://linkedin.com/jobs/view/9182736",
    status: "pending",
    createdAt: daysAgo(0),
    updatedAt: daysAgo(0)
  },
  {
    _id: ObjectId(),
    url: "https://indeed.de/viewjob?jk=abc123xyz",
    status: "pending",
    createdAt: daysAgo(0),
    updatedAt: daysAgo(0)
  }
]);

print("Inserted 4 pending jobs.");

// ── Notifications ──

const guid = () => UUID().toString().replace(/^"|"$/g, "");

db.notifications.insertMany([
  {
    _id: guid(),
    type: "new_application",
    title: "New Application",
    body: "FinEdge Technologies — Software Engineer processed successfully",
    companyName: "FinEdge Technologies",
    companySlug: "finedge-technologies",
    occurredAt: daysAgo(1),
    read: false
  },
  {
    _id: guid(),
    type: "stage_changed",
    title: "Stage Updated",
    body: "Quantum Digital — moved to Interview stage",
    companyName: "Quantum Digital",
    companySlug: "quantum-digital",
    occurredAt: daysAgo(2),
    read: false
  },
  {
    _id: guid(),
    type: "new_application",
    title: "New Application",
    body: "GreenCode Systems — .NET Developer is being processed",
    companyName: "GreenCode Systems",
    companySlug: "greencode-systems",
    occurredAt: daysAgo(0),
    read: false
  },
  {
    _id: guid(),
    type: "stage_changed",
    title: "Offer Received",
    body: "Meridian Software — moved to Offer stage",
    companyName: "Meridian Software",
    companySlug: "meridian-software",
    occurredAt: daysAgo(1),
    read: true
  },
  {
    _id: guid(),
    type: "new_application",
    title: "New Application",
    body: "CloudBridge Solutions — Senior .NET Developer processed successfully",
    companyName: "CloudBridge Solutions",
    companySlug: "cloudbridge-solutions",
    occurredAt: daysAgo(7),
    read: true
  }
]);

print("Inserted 5 notifications.");
print("Done! Staging database seeded successfully.");
