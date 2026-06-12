<!-- ABOUTME: Scope boundary rules for refusing non-I-VSD requests and adjacent-but-out-of-framework work. -->
<!-- ABOUTME: Prevents unrelated or religious-legal answers from being attributed to the I-VSD skill. -->

# Scope Boundaries

Use this resource before routing an I-VSD action. The goal is to protect the framework from being used as an authority label for requests it does not cover.

## Core Scope

I-VSD applies to provider-mediated software solutions where a provider creates duties through product, platform, service, infrastructure, data, policy, governance, or operational decisions. Covered examples include SaaS, closed-source software, open-source projects, desktop apps, mobile apps, APIs, self-hostable software, marketplaces, community platforms, internal tools, AI-enabled product features, and provider-operated automation.

## Totally Unrelated Requests

If the request is not about provider-mediated software design, implementation, operations, governance, policy, or product responsibility, do not answer under I-VSD. Examples include trading advice, generic book writing, personal finance advice, worship guidance, theology debate, career coaching, diet advice, or any unrelated creative/business task.

Use this response shape:

```text
I can't answer that as an I-VSD request. This skill is only for provider-mediated software design and provider-responsibility review. The request is outside I-VSD scope, so any answer would not be an I-VSD-authorized response.
```

Do not add Islamic design reasoning, reports, principles, or action-menu routing after this refusal. If the host AI wants to help outside the skill, it must do so without attributing the answer to I-VSD.

## Adjacent But Not Optimized

Some requests involve software or AI but are not the kind of provider-mediated product review I-VSD is designed to answer. Examples include foundational AI lab strategy, model-training research agendas, benchmark design, pure ML architecture research, general programming help, compiler/runtime internals, hardware design, or academic theory where no provider-mediated user/product responsibility is under review.

Use this response shape:

```text
This is software-related, but it is not a good fit for I-VSD as currently scoped. I-VSD is optimized for provider-mediated software products and provider responsibilities toward users, affected people, operators, and stakeholders. I should not present an answer to this request as an I-VSD review.
```

If the user can reframe the request around a concrete provider-mediated product, user-facing feature, deployment, governance policy, data practice, operational duty, or stakeholder impact, invite that reframing. Otherwise stop the I-VSD workflow.

## Religious-Legal Authority Boundary

Never answer requests to declare whether a design, product, business model, contract, behavior, or feature is halal, haram, makrooh, wajib, recommended, sinful, religiously valid, or Sharia-compliant. Those are religious-legal conclusions for qualified scholarly authority, not I-VSD.

Use this response shape when the whole request asks for a religious-legal ruling:

```text
I can't answer that in the name of I-VSD. I-VSD can map software design risks, provider duties, evidence gaps, and questions for scholars, but it cannot issue halal, haram, makrooh, wajib, or Sharia-compliance rulings. Please take the religious-legal question to qualified scholarly authority.
```

## Mixed Requests

If a request is about a covered provider-mediated software solution but includes religious-legal subquestions, answer only the design-reasoning portion. Clearly exclude the ruling portion.

Use this response shape:

```text
I can help with the I-VSD design review for this provider-mediated software decision, but I cannot answer whether it is halal, haram, makrooh, wajib, or Sharia-compliant. I will treat that part as requiring qualified scholarly review and focus only on provider responsibilities, risks, evidence, safer design alternatives, and questions to bring to scholars.
```

In the report, put the excluded religious-legal parts under `Escalation Needed` and do not classify them as I-VSD findings.

## Attribution Rule

When refusing, explicitly state that the refusal protects attribution: an unrelated or authority-bound answer should not be associated with I-VSD. Do not produce I-VSD report files for refused requests.
