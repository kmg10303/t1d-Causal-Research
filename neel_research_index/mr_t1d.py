import pandas as pd
import numpy as np
import ieugwaspy as gwas
import statsmodels.api as sm

    
# https://opengwas.io/ --> API 

# -------------------------------------------------------------------------
# STEP 1: DEFINE DATA IDS
# Exposure: Cytokine levels (example: Interleukin-6) (Hypothetical OpenGWAS ID: "ieu-a-1055")
# Outcome: Type 1 Diabetes
# -------------------------------------------------------------------------
GWASid = "prot-a-1538"  # ID for Interleukin-6 levels
SNPID = "ebi-a-GCST90014023" # ID for Type 1 Diabetes
def mr_analysis():
    print(f"Fetching instruments for exposure: {GWASid}")
    print(f"Fetching instruments for exposure: {SNPID}")
    # -------------------------------------------------------------------------
    # STEP 2: GET GENETIC INSTRUMENTS
    # We select SNPs associated with the exposure at genome-wide significance
    # -------------------------------------------------------------------------
    
    exposure_gwas = gwas.tophits(GWASid)
    exposure_df = pd.DataFrame(exposure_gwas)
    if exposure_df.empty:
        print("No significant SNPs found for the exposure.")
        return
    SNPs = exposure_df['rsid'].tolist()
    print(f"found genetic instruments: {len(SNPs)}")
    
    # -------------------------------------------------------------------------
    # STEP 3: GET OUTCOME DATA FOR THESE SNPS
    # We need the effect of these specific SNPs on Type 1 Diabetes (ID: "ebi-a-GCST90014023")
    # -------------------------------------------------------------------------
    # Same thing as Step 2, however you don't want to grab the tophits, you want to grab the specific SNPs. 
    outcome_gwas = gwas.associations(SNPID, SNPs)
    outcome_df = pd.DataFrame(outcome_gwas)
    if outcome_df.empty:
        print("No outcome data found for the selected SNPs.")
        return
    
    # Harmonization. Merge exposure and outcome data on SNPs
    merged = pd.merge(
        exposure_df[['rsid', 'beta', 'se', 'effect_allele', 'other_allele']],
        outcome_df[['rsid', 'beta', 'se', 'effect_allele', 'other_allele']],
        on='rsid',
        suffixes=('_exp', '_out')
    )

    # -------------------------------------------------------------------------
    # STEP 4: WALD RATIO & IVW (Inverse Variance Weighted)
    # The causal effect is: Beta_outcome / Beta_exposure
    # -------------------------------------------------------------------------
    # Calculate individual Wald Ratios
    merged['wald_ratio'] = merged['beta_out'] / merged['beta_exp']
    X = merged['beta_exp']
    y = merged['beta_out']

    models = sm.WLS(y, sm.add_constant(X), weights=1/(merged['se_out']**2))
    results = models.fit()

    # -------------------------------------------------------------------------
    # STEP 5: OUTPUT RESULTS
    # -------------------------------------------------------------------------
    print("\n----- MR Results -----")
    # Print out P-value, which is already in results.pvalues


if __name__ == "__main__":
    mr_analysis()


