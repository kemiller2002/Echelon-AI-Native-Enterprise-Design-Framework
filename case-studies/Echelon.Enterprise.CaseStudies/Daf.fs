namespace Echelon.Enterprise.CaseStudies

open Echelon.Enterprise

/// Case Study A: grant recommendation to disbursement at a representative
/// mid-sized donor-advised fund sponsor.
///
/// The current workflow is a COMPOSITE assembled from documented sponsor
/// policies (Fidelity Charitable, Vanguard Charitable, NPT, AEF, RCF/Ren) in
/// the daf-landscape corpus. It is not any one sponsor's process. No sponsor
/// publishes reviewer minutes, exception rates or turnaround distributions
/// (daf-landscape research/DAF-INDUSTRY-REPORT-2026-11.md:220), so every
/// effort and rate below is an assumption or an illustrative value and is
/// labelled as such.
module Daf =

    let private corpus path = Some $"daf-landscape:{path}"

    let private src kind citation locator = {
        Citation = citation
        Locator = locator
        Kind = kind
        Retrieved = None
    }

    let private ev id status confidence statement sources = {
        Id = EvidenceId id
        Statement = statement
        Status = status
        Confidence = confidence
        Sources = sources
        Contradicts = []
    }

    let private report line =
        corpus $"research/DAF-INDUSTRY-REPORT-2026-11.md:{line}"

    let private illustrative =
        src Illustrative "Illustrative modelling value chosen for this case study; not measured." None

    // ---------------------------------------------------------------- evidence

    let evidence: Evidence list = [
        ev
            "EV-DAF-001"
            Fact
            High
            "IRC 4966 imposes a 20% tax on the sponsor for a taxable distribution and 5% (capped at $10k) on a knowing manager."
            [
                src PrimarySource "26 U.S.C. §4966" (Some "https://www.law.cornell.edu/uscode/text/26/4966")
            ]
        ev
            "EV-DAF-002"
            Fact
            High
            "IRC 4967 imposes a 125% tax on a donor or adviser receiving a prohibited benefit and 10% on a knowing manager."
            [
                src PrimarySource "26 U.S.C. §4967" (Some "https://www.law.cornell.edu/uscode/text/26/4967")
            ]
        ev
            "EV-DAF-003"
            Claim
            High
            "Grants to listed §170(b)(1)(A) public charities are excepted from §4966 and grantors may rely on IRS status listings."
            [
                src PrimarySource "26 U.S.C. §4966" (Some "https://www.law.cornell.edu/uscode/text/26/4966")
                src SecondarySource "Rev. Proc. 2018-32 summary, as recorded in the DAF industry report" (report 159)
            ]
        ev
            "EV-DAF-004"
            Fact
            Medium
            "Sponsors review each recommendation for recipient identity, legal eligibility, purpose and private benefit before approval (normalised stages)."
            [
                src
                    SecondarySource
                    "Synthesis of published sponsor policies (Fidelity, Vanguard, DAFgiving360, NPT, AEF)"
                    (corpus "research/batch-2a-charity-vetting-grant-approval-2026-11-12.md:11-43")
            ]
        ev
            "EV-DAF-005"
            Fact
            Medium
            "RCF policy (March 2023): an RCF officer personally reviews and authorises grants over $500,000."
            [
                src
                    PrimarySource
                    "RCF risk policy, March 2023, as recorded in the corpus"
                    (corpus "research/ren-instant-grant-approval-rcf-policy-clarity-audit-2026-11-12.md:24")
            ]
        ev
            "EV-DAF-006"
            Recommendation
            Medium
            "Control rule: AI may add a flag, never clear one; approval authority must stay human or deterministic."
            [
                src Computed "daf-landscape governance analysis of 16 AI failure modes" (report "228-232")
            ]
        ev
            "EV-DAF-007"
            Claim
            Medium
            "Approval cadence conflicts across Ren-administered programmes: trustees approve weekly (UBS circular), daily (NWM circular), and Ren claims over half of grants are approved instantly."
            [
                src
                    VendorClaim
                    "Ren website and programme circulars, as recorded in the corpus"
                    (corpus "research/ren-instant-grant-approval-rcf-policy-clarity-audit-2026-11-12.md:17-21")
            ]
        ev
            "EV-DAF-008"
            Fact
            Medium
            "Operating cost per grant in FY2024 990 data: Fidelity Charitable $68, DAFgiving360 $55, Vanguard Charitable $134, NPT $419."
            [
                src
                    Computed
                    "Computed by daf-landscape from IRS e-file Part IX data"
                    (corpus "research/mission-2026-11-14/sponsor-cost-structure-and-sensitivities.md:13-14")
                src
                    PrimarySource
                    "IRS Form 990 e-file XML (GivingTuesday mirror)"
                    (Some
                        "https://gt990datalake-rawdata.s3.amazonaws.com/EfileData/XmlFiles/202521349349310887_public.xml")
            ]
        ev
            "EV-DAF-009"
            Claim
            Medium
            "Recipients not on standard IRS lists (churches, subordinates, foreign, fiscal sponsors) are 7.4% of grant recipient lines on the core proxy and 25.1% on the broad proxy."
            [
                src
                    Computed
                    "990 Schedule I exception-load proxy (counts recipients, not observed reviews)"
                    (corpus "research/mission-2026-11-14/gap19-exception-load-proxy.md:39-40")
            ]
        ev
            "EV-DAF-010"
            Claim
            Medium
            "Inference costs about $0.45 per heavy document review against about $83 of reviewer time."
            [
                src Computed "daf-landscape AI process economics, using published API and BLS prices" (report 225)
            ]
        ev
            "EV-DAF-011"
            Fact
            Medium
            "Published review timing: Fidelity may take 10+ business days; Vanguard most within 2 business days; NPT 2–5 days; AEF 5–10 business days including an adviser liquidity handoff."
            [
                src
                    PrimarySource
                    "Sponsor grant policy pages, as recorded in the corpus"
                    (corpus "research/batch-2a-charity-vetting-grant-approval-2026-11-12.md:11-15")
            ]
        ev
            "EV-DAF-012"
            Fact
            High
            "No sponsor publishes grant turnaround distributions, exception rates or reviewer minutes."
            [
                src SecondarySource "daf-landscape absence finding, validation wave 21" (report 220)
            ]
        ev
            "EV-DAF-013"
            Assumption
            Low
            "Hands-on minutes, queue delays and exception rates per activity in the composite workflow."
            [ illustrative ]
        ev
            "EV-DAF-014"
            Assumption
            Low
            "Loaded labour cost of $55–$95 per hour for grants operations, due-diligence and compliance roles."
            [ illustrative ]
        ev "EV-DAF-015" Assumption Low "A representative mid-sized sponsor processes 20,000–60,000 grants per year." [
            illustrative
        ]
        ev
            "EV-DAF-016"
            Hypothesis
            Low
            "AI assistance leaves 30–70% of human review effort on cases a human still handles."
            [ illustrative ]
        ev
            "EV-DAF-017"
            Assumption
            Low
            "Platform, evaluation and monitoring costs for the redesign; one-time build cost amortised over 3 years."
            [ illustrative ]
        ev
            "EV-DAF-018"
            Assumption
            VeryLow
            "Probability and cost of an undetected material grant error (taxable distribution, misdirected payment)."
            [
                illustrative
                src
                    Computed
                    "Detection probability of an escaped §4966 error estimated at about 0.1–1% per year"
                    (corpus "research/mission-2026-11-14/g0403-0104-m-detection-ofac.md:22-37")
            ]
        ev
            "EV-DAF-019"
            Fact
            Medium
            "No sponsor publishes a mandatory callback before a charity bank-account change; charity-side bank-change checks cover 29.8% of assets."
            [
                src
                    SecondarySource
                    "daf-landscape fraud and diversion controls review"
                    (corpus "research/mission-2026-11-14/b49-04-fraud-diversion-controls.md:25-33")
            ]
        ev
            "EV-DAF-020"
            Assumption
            Low
            "Agent escalation share, escalation effort, and per-step inference and software costs."
            [
                illustrative
                src Computed "Order-of-magnitude anchor for inference cost" (report 225)
            ]
    ]

    // ------------------------------------------------------------- identifiers

    let private d = DataId
    let grantRecommendation = d "D-GRANT-RECOMMENDATION"
    let fundAccount = d "D-FUND-ACCOUNT"
    let charityReference = d "D-CHARITY-REFERENCE"
    let payeeBank = d "D-PAYEE-BANK-DETAILS"
    let requestValidation = d "D-REQUEST-VALIDATION"
    let eligibility = d "D-ELIGIBILITY-RESULT"
    let purpose = d "D-PURPOSE-ASSESSMENT"
    let approval = d "D-GRANT-APPROVAL"
    let largeGrantAuth = d "D-LARGE-GRANT-AUTHORIZATION"
    let donorNotice = d "D-DONOR-NOTICE"
    let redemption = d "D-REDEMPTION-ORDER"
    let fundsAvailable = d "D-FUNDS-AVAILABLE"
    let payeeVerification = d "D-PAYEE-VERIFICATION"
    let payment = d "D-PAYMENT"
    let remittance = d "D-RECIPIENT-REMITTANCE"
    let reconciliation = d "D-RECONCILIATION"
    let grantRecord = d "D-GRANT-RECORD"
    let scheduleI = d "D-SCHEDULE-I-EXTRACT"

    let associate = RoleId "R-GRANTS-ASSOCIATE"
    let analyst = RoleId "R-DUE-DILIGENCE-ANALYST"
    let compliance = RoleId "R-COMPLIANCE-OFFICER"
    let officer = RoleId "R-SPONSOR-OFFICER"
    let treasury = RoleId "R-TREASURY-OPERATIONS"
    let accountant = RoleId "R-FUND-ACCOUNTANT"

    let private cEligible = ConstraintId "C-4966-ELIGIBLE-RECIPIENT"
    let private cBenefit = ConstraintId "C-4967-NO-PROHIBITED-BENEFIT"
    let private cSanctions = ConstraintId "C-OFAC-SANCTIONS"
    let private cControl = ConstraintId "C-SPONSOR-LEGAL-CONTROL"
    let private cLarge = ConstraintId "C-LARGE-GRANT-AUTHORITY"
    let private cPayee = ConstraintId "C-PAYEE-VERIFICATION"
    let private cFunds = ConstraintId "C-AVAILABLE-BALANCE"

    // -------------------------------------------------------------- estimates

    let private assumed = [ EvidenceId "EV-DAF-013" ]
    let private mins lo ex hi : Estimate<minute> = Estimate.create lo ex hi assumed
    let private rate lo ex hi : Estimate<1> = Estimate.create lo ex hi assumed
    let private none = Estimate.exactly 0.0<minute> assumed
    let private noExceptions = Estimate.exactly 0.0 assumed

    let private structured = {
        RuleSpecifiable = true
        InputStructure = Structured
        Judgment = Negligible
        Reversibility = Reversible
        FinancialExposure = Minor
        ExplainabilityRequired = false
        ClearsRisk = false
    }

    let private activity id name nature performer inputs outputs satisfies effort delay exceptions profile = {
        Id = ActivityId id
        Name = name
        Nature = nature
        Performer = performer
        Inputs = inputs
        Outputs = outputs
        Satisfies = satisfies
        Effort = effort
        Delay = delay
        ExceptionRate = exceptions
        Profile = profile
        Evidence = [ EvidenceId "EV-DAF-004"; EvidenceId "EV-DAF-013" ]
    }

    // --------------------------------------------------------------- workflow

    let currentWorkflow: Workflow = {
        Id = WorkflowId "WF-DAF-GRANT"
        Name = "Grant recommendation to disbursement"
        Outcome = OutcomeId "OUT-GRANT-DELIVERED"
        Trigger = "Donor or adviser submits a grant recommendation"
        Inputs = [ grantRecommendation; fundAccount; charityReference; payeeBank ]
        Governing = [ cEligible; cBenefit; cSanctions; cControl; cLarge; cPayee; cFunds ]
        Owner = associate
        Completion = [
            "Payment settled with an eligible recipient"
            "Recipient has remittance detail identifying the grant"
            "Grant record retained with every control's evidence"
        ]
        Evidence = [ EvidenceId "EV-DAF-004"; EvidenceId "EV-DAF-011" ]
        Activities = [
            activity
                "A01"
                "Re-key recommendation from portal or email into the grants system"
                Transfer
                (Human associate)
                [ grantRecommendation ]
                [ grantRecommendation ]
                []
                (mins 2.0<minute> 4.0<minute> 8.0<minute>)
                (mins 60.0<minute> 240.0<minute> 960.0<minute>)
                (rate 0.01 0.03 0.06)
                structured
            activity
                "A02"
                "Validate adviser authority, fund balance, amount and required fields"
                Verify
                (Human associate)
                [ grantRecommendation; fundAccount ]
                [ requestValidation ]
                [ cFunds ]
                (mins 2.0<minute> 3.0<minute> 6.0<minute>)
                none
                (rate 0.02 0.05 0.10)
                structured
            activity
                "A03"
                "Return incomplete recommendations to the donor and re-enter them"
                Rework
                (Human associate)
                [ grantRecommendation; requestValidation ]
                [ grantRecommendation ]
                []
                (mins 0.5<minute> 1.0<minute> 3.0<minute>)
                (mins 0.0<minute> 120.0<minute> 1440.0<minute>)
                noExceptions
                structured
            activity
                "A04"
                "Queue for due diligence"
                Wait
                (Human analyst)
                []
                []
                []
                none
                (mins 240.0<minute> 960.0<minute> 2880.0<minute>)
                noExceptions
                structured
            activity
                "A05"
                "Verify recipient identity, IRS status (BMF, Pub 78, auto-revocation) and OFAC"
                Verify
                (Human analyst)
                [ grantRecommendation; charityReference ]
                [ eligibility ]
                [ cEligible; cSanctions ]
                (mins 3.0<minute> 6.0<minute> 15.0<minute>)
                none
                (rate 0.07 0.10 0.25)
                {
                    structured with
                        FinancialExposure = Moderate
                }
            activity
                "A06"
                "Review grant purpose and prohibited or private benefit"
                Verify
                (Human analyst)
                [ grantRecommendation; eligibility ]
                [ purpose ]
                [ cBenefit ]
                (mins 3.0<minute> 8.0<minute> 20.0<minute>)
                none
                (rate 0.03 0.08 0.15)
                {
                    RuleSpecifiable = false
                    InputStructure = SemiStructured
                    Judgment = Moderate
                    Reversibility = Reversible
                    FinancialExposure = Moderate
                    ExplainabilityRequired = true
                    ClearsRisk = true
                }
            activity
                "A07"
                "Forward flagged cases to compliance by email"
                Transfer
                (Human analyst)
                [ purpose ]
                [ purpose ]
                []
                (mins 0.2<minute> 0.5<minute> 1.0<minute>)
                (mins 60.0<minute> 480.0<minute> 1440.0<minute>)
                noExceptions
                structured
            activity
                "A08"
                "Compliance re-verifies eligibility on forwarded cases"
                Verify
                (Human compliance)
                [ grantRecommendation; charityReference ]
                [ eligibility ]
                [ cEligible; cSanctions ]
                (mins 0.5<minute> 1.5<minute> 4.0<minute>)
                none
                (rate 0.0 0.02 0.05)
                {
                    structured with
                        FinancialExposure = Moderate
                }
            activity
                "A09"
                "Wait for the scheduled approval batch"
                Wait
                (Human officer)
                []
                []
                []
                none
                (mins 480.0<minute> 2400.0<minute> 7200.0<minute>)
                noExceptions
                structured
            activity
                "A10"
                "Approve or deny the grant under sponsor policy"
                (Decide(DecisionId "DEC-APPROVE-GRANT"))
                (Human compliance)
                [ requestValidation; eligibility; purpose ]
                [ approval ]
                [ cControl ]
                (mins 1.0<minute> 2.0<minute> 5.0<minute>)
                none
                (rate 0.01 0.03 0.08)
                {
                    structured with
                        FinancialExposure = Major
                }
            activity
                "A11"
                "Officer authorises grants above the large-grant threshold"
                (Decide(DecisionId "DEC-AUTHORIZE-LARGE-GRANT"))
                (Human officer)
                [ approval ]
                [ largeGrantAuth ]
                [ cLarge ]
                // Averaged over all grants: ~1% exceed the threshold at ~20 minutes each.
                (mins 0.05<minute> 0.2<minute> 0.5<minute>)
                none
                noExceptions
                {
                    structured with
                        RuleSpecifiable = false
                        Judgment = Major
                        FinancialExposure = Major
                        ExplainabilityRequired = true
                }
            activity
                "A12"
                "Email the donor that the grant was approved"
                Notify
                (Software "grants system")
                [ approval ]
                [ donorNotice ]
                []
                none
                none
                noExceptions
                structured
            activity
                "A13"
                "Place a redemption order to raise cash for the grant"
                Produce
                (Human treasury)
                [ approval; fundAccount ]
                [ redemption ]
                []
                (mins 1.0<minute> 2.0<minute> 5.0<minute>)
                (mins 60.0<minute> 480.0<minute> 1440.0<minute>)
                (rate 0.0 0.01 0.03)
                structured
            activity
                "A14"
                "Wait for the custodian to settle the redemption"
                Wait
                (ExternalParty "custodian")
                []
                []
                []
                none
                (mins 1440.0<minute> 1440.0<minute> 2880.0<minute>)
                noExceptions
                structured
            activity
                "A15"
                "Custodian confirms settled cash is available"
                Produce
                (ExternalParty "custodian")
                [ redemption ]
                [ fundsAvailable ]
                []
                none
                none
                noExceptions
                structured
            activity
                "A16"
                "Verify the payee bank account independently of the grant preparer"
                Verify
                (Human treasury)
                [ payeeBank ]
                [ payeeVerification ]
                [ cPayee ]
                (mins 0.5<minute> 1.0<minute> 3.0<minute>)
                none
                (rate 0.01 0.02 0.05)
                {
                    structured with
                        FinancialExposure = Major
                }
            activity
                "A17"
                "Re-key the approved payment into the bank portal"
                Transfer
                (Human treasury)
                [ approval ]
                [ approval ]
                []
                (mins 1.0<minute> 2.0<minute> 4.0<minute>)
                (mins 60.0<minute> 480.0<minute> 1440.0<minute>)
                (rate 0.0 0.01 0.02)
                structured
            activity
                "A18"
                "Release payment (ACH, check or processor)"
                Produce
                (Human treasury)
                [ approval; largeGrantAuth; fundsAvailable; payeeVerification ]
                [ payment ]
                []
                (mins 0.5<minute> 1.0<minute> 2.0<minute>)
                none
                (rate 0.0 0.01 0.03)
                {
                    structured with
                        Reversibility = Irreversible
                        FinancialExposure = Major
                }
            activity
                "A19"
                "Send the recipient a grant letter with remittance detail"
                Notify
                (Human associate)
                [ payment; grantRecommendation ]
                [ remittance ]
                []
                (mins 1.0<minute> 2.0<minute> 4.0<minute>)
                none
                noExceptions
                structured
            activity
                "A20"
                "Wait for the recipient to deposit or claim the payment"
                Wait
                (ExternalParty "recipient charity")
                []
                []
                []
                none
                (mins 1440.0<minute> 4320.0<minute> 20160.0<minute>)
                noExceptions
                structured
            activity
                "A21"
                "Reconcile the payment against the ledger and file the grant record"
                Verify
                (Human accountant)
                [ payment; approval ]
                [ reconciliation; grantRecord ]
                [ cControl ]
                (mins 1.0<minute> 2.0<minute> 5.0<minute>)
                (mins 1440.0<minute> 4320.0<minute> 10080.0<minute>)
                (rate 0.01 0.03 0.06)
                structured
            activity
                "A22"
                "Compile a monthly Schedule I spreadsheet from the grant records"
                Produce
                (Human accountant)
                [ grantRecord ]
                [ scheduleI ]
                []
                (mins 0.2<minute> 0.5<minute> 1.0<minute>)
                none
                noExceptions
                structured
        ]
    }

    // ------------------------------------------------------------------ model

    let private role id name lo ex hi = {
        Id = RoleId id
        Name = name
        Kind = InternalRole
        LoadedCostPerHour = Estimate.create lo ex hi [ EvidenceId "EV-DAF-014" ]
    }

    let private dataItem id name cls = {
        Id = id
        Name = name
        Classification = cls
    }

    let private usdE lo ex hi ev : Estimate<usd> =
        Estimate.create lo ex hi [ EvidenceId ev ]

    let model: EnterpriseModel = {
        Name = "Representative DAF sponsor: grant disbursement"
        Industry = "Donor-advised funds (US)"
        Scope = "Grant recommendation through payment, reconciliation and record retention"
        Stakeholders = [
            {
                Id = StakeholderId "SH-DONOR"
                Name = "Donor or adviser"
                Needs = [
                    "Recommended grants reach the intended charity promptly"
                    "Visibility of status"
                ]
            }
            {
                Id = StakeholderId "SH-CHARITY"
                Name = "Recipient charity"
                Needs = [
                    "Receive funds reliably"
                    "Know which grant and purpose a payment relates to"
                ]
            }
            {
                Id = StakeholderId "SH-SPONSOR"
                Name = "Sponsoring organisation and its managers"
                Needs = [
                    "Exercise and evidence legal control"
                    "Avoid §4966/§4967 taxes"
                    "Low cost per grant"
                ]
            }
            {
                Id = StakeholderId "SH-REGULATOR"
                Name = "IRS and state attorneys general"
                Needs = [ "Charitable use of funds"; "Accurate Form 990 reporting" ]
            }
        ]
        Outcomes = [
            {
                Id = OutcomeId "OUT-GRANT-DELIVERED"
                Statement =
                    "An eligible charity receives the recommended grant for a charitable purpose, with the sponsor's legal control exercised and evidenced."
                For = [
                    StakeholderId "SH-DONOR"
                    StakeholderId "SH-CHARITY"
                    StakeholderId "SH-SPONSOR"
                    StakeholderId "SH-REGULATOR"
                ]
                RequiredInformation = [ payment; remittance; grantRecord ]
                Measures = [
                    "Recommendation-to-settlement time (T2→T6)"
                    "Taxable distributions per 10,000 grants"
                    "Cost per settled grant"
                ]
                Evidence = [ EvidenceId "EV-DAF-004" ]
            }
        ]
        Constraints = [
            {
                Id = cEligible
                Kind = Regulatory
                Statement = "Grants must go to eligible recipients or carry expenditure responsibility (§4966)."
                Authority = "26 U.S.C. §4966"
                Obligations = [ RequiresInformation charityReference; RetainRecord eligibility ]
                Evidence = [ EvidenceId "EV-DAF-001"; EvidenceId "EV-DAF-003" ]
            }
            {
                Id = cBenefit
                Kind = Regulatory
                Statement = "No more-than-incidental benefit to a donor, adviser or related person (§4967)."
                Authority = "26 U.S.C. §4967; §4958(c)(2)"
                Obligations = [ NoAiClearance; RetainRecord purpose ]
                Evidence = [ EvidenceId "EV-DAF-002"; EvidenceId "EV-DAF-006" ]
            }
            {
                Id = cSanctions
                Kind = Regulatory
                Statement = "Recipients must be screened against OFAC sanctions lists."
                Authority = "OFAC regulations; Treasury charity risk guidance"
                Obligations = [ RequiresInformation charityReference ]
                Evidence = [ EvidenceId "EV-DAF-004" ]
            }
            {
                Id = cControl
                Kind = Fiduciary
                Statement =
                    "The sponsor exercises legal control: approval is a sponsor decision, never inferred from eligibility or funding, and is evidenced."
                Authority = "IRC §4966(d)(2); sponsor grant policy"
                Obligations = [ RetainRecord grantRecord; RequiresInformation purpose ]
                Evidence = [ EvidenceId "EV-DAF-004"; EvidenceId "EV-DAF-006" ]
            }
            {
                Id = cLarge
                Kind = Policy
                Statement = "An officer personally authorises grants above the large-grant threshold."
                Authority = "Sponsor risk policy (cf. RCF March 2023, $500k)"
                Obligations = [ HumanAuthorization officer ]
                Evidence = [ EvidenceId "EV-DAF-005" ]
            }
            {
                Id = cPayee
                Kind = Policy
                Statement = "Payee bank details are verified independently of whoever prepared the grant."
                Authority = "Sponsor treasury controls (segregation of duties)"
                Obligations = [ IndependentCheck; RequiresInformation payeeBank ]
                Evidence = [ EvidenceId "EV-DAF-019" ]
            }
            {
                Id = cFunds
                Kind = Financial
                Statement =
                    "A grant may not exceed the fund's available balance and must come from an authorised adviser."
                Authority = "Sponsor programme terms"
                Obligations = [ RequiresInformation fundAccount ]
                Evidence = [ EvidenceId "EV-DAF-004" ]
            }
        ]
        Capabilities = [
            {
                Id = CapabilityId "CAP-GRANT-INTAKE"
                Name = "Grant intake"
                Domain = "Grantmaking"
                Purpose = "Capture complete, authorised grant recommendations once."
                Supports = [ OutcomeId "OUT-GRANT-DELIVERED" ]
                Owns = [ grantRecommendation; requestValidation; donorNotice ]
            }
            {
                Id = CapabilityId "CAP-RECIPIENT-DILIGENCE"
                Name = "Recipient diligence"
                Domain = "Compliance"
                Purpose = "Establish recipient eligibility, sanctions status and grant purpose."
                Supports = [ OutcomeId "OUT-GRANT-DELIVERED" ]
                Owns = [ charityReference; eligibility; purpose ]
            }
            {
                Id = CapabilityId "CAP-GRANT-AUTHORIZATION"
                Name = "Grant authorisation"
                Domain = "Governance"
                Purpose = "Record sponsor decisions and delegated authority."
                Supports = [ OutcomeId "OUT-GRANT-DELIVERED" ]
                Owns = [ approval; largeGrantAuth ]
            }
            {
                Id = CapabilityId "CAP-FUND-LIQUIDITY"
                Name = "Fund liquidity"
                Domain = "Investments"
                Purpose = "Make settled cash available for approved grants."
                Supports = [ OutcomeId "OUT-GRANT-DELIVERED" ]
                Owns = [ fundAccount; redemption; fundsAvailable ]
            }
            {
                Id = CapabilityId "CAP-DISBURSEMENT"
                Name = "Disbursement"
                Domain = "Treasury"
                Purpose = "Pay verified payees and tell them what the payment is for."
                Supports = [ OutcomeId "OUT-GRANT-DELIVERED" ]
                Owns = [ payeeBank; payeeVerification; payment; remittance ]
            }
            {
                Id = CapabilityId "CAP-FUND-ACCOUNTING"
                Name = "Fund accounting and records"
                Domain = "Finance"
                Purpose = "Reconcile, retain audit evidence and supply regulatory reporting."
                Supports = [ OutcomeId "OUT-GRANT-DELIVERED" ]
                Owns = [ reconciliation; grantRecord; scheduleI ]
            }
        ]
        Data = [
            dataItem grantRecommendation "Grant recommendation" ConfidentialData
            dataItem fundAccount "Fund account" ConfidentialData
            dataItem charityReference "Charity reference data" PublicData
            dataItem payeeBank "Payee bank details" RestrictedData
            dataItem requestValidation "Request validation" InternalData
            dataItem eligibility "Eligibility result" InternalData
            dataItem purpose "Purpose assessment" ConfidentialData
            dataItem approval "Grant approval" InternalData
            dataItem largeGrantAuth "Large grant authorization" InternalData
            dataItem donorNotice "Donor notice" ConfidentialData
            dataItem redemption "Redemption order" InternalData
            dataItem fundsAvailable "Funds available" InternalData
            dataItem payeeVerification "Payee verification" RestrictedData
            dataItem payment "Payment" ConfidentialData
            dataItem remittance "Recipient remittance" InternalData
            dataItem reconciliation "Reconciliation" InternalData
            dataItem grantRecord "Grant record" ConfidentialData
            dataItem scheduleI "Schedule I extract" InternalData
        ]
        Roles = [
            role "R-GRANTS-ASSOCIATE" "Grants operations associate" 50.0<usd / hour> 60.0<usd / hour> 75.0<usd / hour>
            role "R-DUE-DILIGENCE-ANALYST" "Due-diligence analyst" 60.0<usd / hour> 75.0<usd / hour> 95.0<usd / hour>
            role "R-COMPLIANCE-OFFICER" "Compliance officer" 70.0<usd / hour> 85.0<usd / hour> 110.0<usd / hour>
            role "R-SPONSOR-OFFICER" "Sponsor officer" 100.0<usd / hour> 130.0<usd / hour> 180.0<usd / hour>
            role "R-TREASURY-OPERATIONS" "Treasury operations" 55.0<usd / hour> 65.0<usd / hour> 80.0<usd / hour>
            role "R-FUND-ACCOUNTANT" "Fund accountant" 55.0<usd / hour> 70.0<usd / hour> 85.0<usd / hour>
        ]
        Decisions = [
            {
                Id = DecisionId "DEC-APPROVE-GRANT"
                Question = "Does the sponsor approve this grant under its policy?"
                Inputs = [ requestValidation; eligibility; purpose ]
                Output = approval
                Evidence = [ EvidenceId "EV-DAF-004" ]
            }
            {
                Id = DecisionId "DEC-AUTHORIZE-LARGE-GRANT"
                Question = "Does an officer authorise this large grant?"
                Inputs = [ approval ]
                Output = largeGrantAuth
                Evidence = [ EvidenceId "EV-DAF-005" ]
            }
        ]
        Workflows = [ currentWorkflow ]
        Evidence =
            evidence
            |> List.map (fun e ->
                if e.Id = EvidenceId "EV-DAF-007" then
                    {
                        e with
                            Contradicts = [ EvidenceId "EV-DAF-011" ]
                    }
                else
                    e)
        Economics = {
            AnnualVolume = Estimate.create 20000.0<txn> 40000.0<txn> 60000.0<txn> [ EvidenceId "EV-DAF-015" ]
            AssistedEffortFactor = Estimate.create 0.3 0.5 0.7 [ EvidenceId "EV-DAF-016" ]
            AgentEscalationRate = Estimate.create 0.05 0.12 0.25 [ EvidenceId "EV-DAF-020" ]
            EscalationEffort = Estimate.create 3.0<minute> 6.0<minute> 12.0<minute> [ EvidenceId "EV-DAF-020" ]
            InferenceCostPerAgentStep = usdE 0.02<usd> 0.10<usd> 0.50<usd> "EV-DAF-020"
            InferenceCostPerAssistedStep = usdE 0.02<usd> 0.10<usd> 0.50<usd> "EV-DAF-020"
            DeterministicCostPerStep = usdE 0.001<usd> 0.01<usd> 0.05<usd> "EV-DAF-020"
            CurrentFixedAnnual = usdE 60000.0<usd> 90000.0<usd> 120000.0<usd> "EV-DAF-017"
            RedesignFixedAnnual = usdE 100000.0<usd> 160000.0<usd> 250000.0<usd> "EV-DAF-017"
            RedesignOneTimeCost = usdE 250000.0<usd> 400000.0<usd> 750000.0<usd> "EV-DAF-017"
            AmortizationYears = 3.0
            CurrentErrorRate = Estimate.create 0.0001 0.0005 0.002 [ EvidenceId "EV-DAF-018" ]
            RedesignErrorRate = Estimate.create 0.00005 0.0003 0.002 [ EvidenceId "EV-DAF-018" ]
            LossPerError = usdE 1000.0<usd> 5000.0<usd> 25000.0<usd> "EV-DAF-018"
        }
    }
