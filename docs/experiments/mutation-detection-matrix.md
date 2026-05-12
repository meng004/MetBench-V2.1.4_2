# Mutation-detection matrix

MR factor: 1.5. Status `not-affected` means the mutation patches a file the scenario does not exercise; status `ran` reports detected/missed; status `error` reports a runtime failure.


## Per-MR detection rate (Wilson 95% CI)

| Scenario | n (semantic mutants affecting it) | detected | missed | errors | rate | 95% CI |
|---|---|---|---|---|---|---|
| openmoc-pincell-nu-sigma-f | 16 | 7 | 9 | 0 | 43.8% | [23.1%, 66.8%] |
| openmc-pincell-nu-sigma-f | 14 | 4 | 10 | 0 | 28.6% | [11.7%, 54.6%] |
| openmoc-pincell-sigma-a | 14 | 4 | 10 | 0 | 28.6% | [11.7%, 54.6%] |
| openmc-pincell-sigma-a | 11 | 3 | 8 | 0 | 27.3% | [9.7%, 56.6%] |
| openmoc-pincell-group-permute | 12 | 4 | 8 | 0 | 33.3% | [13.8%, 60.9%] |
| openmoc-pincell-fuel-sigma-t | 11 | 1 | 10 | 0 | 9.1% | [1.6%, 37.7%] |
| openmoc-pincell-moderator-sigma-a | 12 | 3 | 9 | 0 | 25.0% | [8.9%, 53.2%] |
| openmc-pincell-group-permute | 11 | 3 | 8 | 0 | 27.3% | [9.7%, 56.6%] |
| openmc-pincell-fuel-sigma-t | 10 | 1 | 9 | 0 | 10.0% | [1.8%, 40.4%] |
| openmc-pincell-moderator-sigma-a | 10 | 3 | 7 | 0 | 30.0% | [10.8%, 60.3%] |
| openmoc-pincell-fuel-sigma-s | 12 | 5 | 7 | 0 | 41.7% | [19.3%, 68.0%] |
| openmc-pincell-fuel-sigma-s | 11 | 4 | 7 | 0 | 36.4% | [15.2%, 64.6%] |
| openmoc-pincell-fuel-radius | 12 | 5 | 7 | 0 | 41.7% | [19.3%, 68.0%] |
| openmc-pincell-fuel-radius | 11 | 3 | 8 | 0 | 27.3% | [9.7%, 56.6%] |
| openmc-pincell-particles-refine | 9 | 3 | 6 | 0 | 33.3% | [12.1%, 64.6%] |
| openmoc-pincell-rotate-90 | 4 | 1 | 3 | 0 | 25.0% | [4.6%, 69.9%] |
| openmc-pincell-rotate-90 | 9 | 2 | 7 | 0 | 22.2% | [6.3%, 54.7%] |
| openmoc-pincell-mirror-x | 3 | 1 | 2 | 0 | 33.3% | [6.1%, 79.2%] |
| openmc-pincell-mirror-x | 3 | 0 | 3 | 0 | 0.0% | [0.0%, 56.2%] |
| openmoc-pincell-mirror-y | 3 | 1 | 2 | 0 | 33.3% | [6.1%, 79.2%] |
| openmc-pincell-mirror-y | 3 | 0 | 3 | 0 | 0.0% | [0.0%, 56.2%] |
| openmoc-pincell-fuel-temperature | 1 | 1 | 0 | 0 | 100.0% | [20.7%, 100.0%] |
| openmc-pincell-fuel-temperature | 1 | 1 | 0 | 0 | 100.0% | [20.7%, 100.0%] |

## Identity false-positive sanity

Mut00 (identity) detected on 0 / 23 scenarios. Expected 0 — ✓ PASS.


## Per-mutation detail

| mutation | scenario | outcome | k_source | k_followup | ratio |
|---|---|---|---|---|---|
| Mut00-identity | openmoc-pincell-nu-sigma-f | missed | 1.13306 | 1.69990 | 1.50028 |
| Mut00-identity | openmc-pincell-nu-sigma-f | missed | 1.12450 | 1.68996 | 1.50286 |
| Mut00-identity | openmoc-pincell-sigma-a | missed | 1.13306 | 0.80690 | 0.71215 |
| Mut00-identity | openmc-pincell-sigma-a | missed | 1.12450 | 0.80272 | 0.71384 |
| Mut00-identity | openmoc-pincell-group-permute | missed | 1.13306 | 1.13306 | 1.00000 |
| Mut00-identity | openmoc-pincell-fuel-sigma-t | missed | 1.13306 | 0.11127 | 0.09821 |
| Mut00-identity | openmoc-pincell-moderator-sigma-a | missed | 1.13306 | 0.47635 | 0.42041 |
| Mut00-identity | openmc-pincell-group-permute | missed | 1.12450 | 1.12601 | 1.00134 |
| Mut00-identity | openmc-pincell-fuel-sigma-t | missed | 1.12450 | 0.11292 | 0.10042 |
| Mut00-identity | openmc-pincell-moderator-sigma-a | missed | 1.12450 | 0.96831 | 0.86110 |
| Mut00-identity | openmoc-pincell-fuel-sigma-s | missed | 1.13306 | 1.09185 | 0.96363 |
| Mut00-identity | openmc-pincell-fuel-sigma-s | missed | 1.12450 | 1.09326 | 0.97222 |
| Mut00-identity | openmoc-pincell-fuel-radius | missed | 1.13306 | 1.17476 | 1.03681 |
| Mut00-identity | openmc-pincell-fuel-radius | missed | 1.12450 | 1.17086 | 1.04123 |
| Mut00-identity | openmc-pincell-particles-refine | missed | 1.12450 | 1.12477 | 1.00024 |
| Mut00-identity | openmoc-pincell-rotate-90 | missed | 1.15950 | 1.15949 | 0.99999 |
| Mut00-identity | openmc-pincell-rotate-90 | missed | 1.15438 | 1.15354 | 0.99927 |
| Mut00-identity | openmoc-pincell-mirror-x | missed | 1.10271 | 1.10270 | 0.99999 |
| Mut00-identity | openmc-pincell-mirror-x | missed | 1.09602 | 1.09808 | 1.00188 |
| Mut00-identity | openmoc-pincell-mirror-y | missed | 1.10271 | 1.10271 | 1.00000 |
| Mut00-identity | openmc-pincell-mirror-y | missed | 1.09602 | 1.09555 | 0.99958 |
| Mut00-identity | openmoc-pincell-fuel-temperature | missed | 1.13306 | 1.11566 | 0.98464 |
| Mut00-identity | openmc-pincell-fuel-temperature | missed | 1.12450 | 1.10966 | 0.98680 |
| Mut01-openmoc-runner-chi-zero | openmoc-pincell-nu-sigma-f | detected | 0.00059 | 0.00059 | 1.00000 |
| Mut01-openmoc-runner-chi-zero | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut01-openmoc-runner-chi-zero | openmoc-pincell-sigma-a | missed | 0.00059 | 0.00051 | 0.85942 |
| Mut01-openmoc-runner-chi-zero | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut01-openmoc-runner-chi-zero | openmoc-pincell-group-permute | missed | 0.00059 | 0.00059 | 1.00000 |
| Mut01-openmoc-runner-chi-zero | openmoc-pincell-fuel-sigma-t | missed | 0.00059 | 0.00017 | 0.28482 |
| Mut01-openmoc-runner-chi-zero | openmoc-pincell-moderator-sigma-a | missed | 0.00059 | 0.00056 | 0.94045 |
| Mut01-openmoc-runner-chi-zero | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut01-openmoc-runner-chi-zero | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut01-openmoc-runner-chi-zero | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut01-openmoc-runner-chi-zero | openmoc-pincell-fuel-sigma-s | detected | 0.00059 | 0.00065 | 1.10388 |
| Mut01-openmoc-runner-chi-zero | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut01-openmoc-runner-chi-zero | openmoc-pincell-fuel-radius | detected | 0.00059 | 0.00058 | 0.97244 |
| Mut01-openmoc-runner-chi-zero | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut01-openmoc-runner-chi-zero | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut02-openmoc-runner-sigt-from-siga | openmoc-pincell-nu-sigma-f | detected | inf | inf | nan |
| Mut02-openmoc-runner-sigt-from-siga | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut02-openmoc-runner-sigt-from-siga | openmoc-pincell-sigma-a | detected | inf | inf | nan |
| Mut02-openmoc-runner-sigt-from-siga | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut02-openmoc-runner-sigt-from-siga | openmoc-pincell-group-permute | detected | inf | inf | nan |
| Mut02-openmoc-runner-sigt-from-siga | openmoc-pincell-fuel-sigma-t | missed | inf | 6516098970603957888316380008382948861587503041313665468271873011331664650441856767639667651003029937522954196416950526036577970579995270752506212855928128249917601734336423190876341916486010151831845950378483505420514663009087276449935759692059365571874124672531400508053630782935944807432533213542076121088.00000 | 0.00000 |
| Mut02-openmoc-runner-sigt-from-siga | openmoc-pincell-moderator-sigma-a | detected | inf | inf | nan |
| Mut02-openmoc-runner-sigt-from-siga | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut02-openmoc-runner-sigt-from-siga | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut02-openmoc-runner-sigt-from-siga | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut02-openmoc-runner-sigt-from-siga | openmoc-pincell-fuel-sigma-s | detected | inf | inf | nan |
| Mut02-openmoc-runner-sigt-from-siga | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut02-openmoc-runner-sigt-from-siga | openmoc-pincell-fuel-radius | detected | inf | inf | nan |
| Mut02-openmoc-runner-sigt-from-siga | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut02-openmoc-runner-sigt-from-siga | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut03-openmoc-runner-swap-fuel-moderator | openmoc-pincell-nu-sigma-f | missed | 1.43300 | 2.14972 | 1.50015 |
| Mut03-openmoc-runner-swap-fuel-moderator | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut03-openmoc-runner-swap-fuel-moderator | openmoc-pincell-sigma-a | missed | 1.43300 | 0.91350 | 0.63747 |
| Mut03-openmoc-runner-swap-fuel-moderator | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut03-openmoc-runner-swap-fuel-moderator | openmoc-pincell-group-permute | missed | 1.43300 | 1.43300 | 1.00000 |
| Mut03-openmoc-runner-swap-fuel-moderator | openmoc-pincell-fuel-sigma-t | missed | 1.43300 | 0.09171 | 0.06400 |
| Mut03-openmoc-runner-swap-fuel-moderator | openmoc-pincell-moderator-sigma-a | missed | 1.43300 | 1.36788 | 0.95456 |
| Mut03-openmoc-runner-swap-fuel-moderator | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut03-openmoc-runner-swap-fuel-moderator | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut03-openmoc-runner-swap-fuel-moderator | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut03-openmoc-runner-swap-fuel-moderator | openmoc-pincell-fuel-sigma-s | missed | 1.43300 | 1.28766 | 0.89858 |
| Mut03-openmoc-runner-swap-fuel-moderator | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut03-openmoc-runner-swap-fuel-moderator | openmoc-pincell-fuel-radius | detected | 1.43300 | 0.48497 | 0.33843 |
| Mut03-openmoc-runner-swap-fuel-moderator | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut03-openmoc-runner-swap-fuel-moderator | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmoc-pincell-nu-sigma-f | detected | nan | nan | nan |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmoc-pincell-sigma-a | detected | nan | nan | nan |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmoc-pincell-group-permute | detected | nan | nan | nan |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmoc-pincell-fuel-sigma-t | detected | nan | nan | nan |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmoc-pincell-moderator-sigma-a | detected | nan | nan | nan |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmoc-pincell-fuel-sigma-s | detected | nan | nan | nan |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmoc-pincell-fuel-radius | detected | nan | nan | nan |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut04-openmoc-runner-drop-nu-sigma-f | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut05-openmoc-runner-chi-swap-groups | openmoc-pincell-nu-sigma-f | missed | 1.27800 | 1.91687 | 1.49990 |
| Mut05-openmoc-runner-chi-swap-groups | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut05-openmoc-runner-chi-swap-groups | openmoc-pincell-sigma-a | missed | 1.27800 | 0.96468 | 0.75484 |
| Mut05-openmoc-runner-chi-swap-groups | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut05-openmoc-runner-chi-swap-groups | openmoc-pincell-group-permute | missed | 1.27800 | 1.27800 | 1.00000 |
| Mut05-openmoc-runner-chi-swap-groups | openmoc-pincell-fuel-sigma-t | missed | 1.27800 | 0.29048 | 0.22729 |
| Mut05-openmoc-runner-chi-swap-groups | openmoc-pincell-moderator-sigma-a | missed | 1.27800 | 1.09441 | 0.85635 |
| Mut05-openmoc-runner-chi-swap-groups | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut05-openmoc-runner-chi-swap-groups | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut05-openmoc-runner-chi-swap-groups | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut05-openmoc-runner-chi-swap-groups | openmoc-pincell-fuel-sigma-s | detected | 1.27800 | 1.27814 | 1.00011 |
| Mut05-openmoc-runner-chi-swap-groups | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut05-openmoc-runner-chi-swap-groups | openmoc-pincell-fuel-radius | missed | 1.27800 | 1.33609 | 1.04546 |
| Mut05-openmoc-runner-chi-swap-groups | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut05-openmoc-runner-chi-swap-groups | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut06-openmoc-runner-vacuum-boundary | openmoc-pincell-nu-sigma-f | missed | 0.00470 | 0.00704 | 1.49898 |
| Mut06-openmoc-runner-vacuum-boundary | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut06-openmoc-runner-vacuum-boundary | openmoc-pincell-sigma-a | missed | 0.00470 | 0.00466 | 0.99057 |
| Mut06-openmoc-runner-vacuum-boundary | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut06-openmoc-runner-vacuum-boundary | openmoc-pincell-group-permute | missed | 0.00470 | 0.00470 | 1.00000 |
| Mut06-openmoc-runner-vacuum-boundary | openmoc-pincell-fuel-sigma-t | missed | 0.00470 | 0.00420 | 0.89288 |
| Mut06-openmoc-runner-vacuum-boundary | openmoc-pincell-moderator-sigma-a | missed | 0.00470 | 0.00470 | 0.99937 |
| Mut06-openmoc-runner-vacuum-boundary | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut06-openmoc-runner-vacuum-boundary | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut06-openmoc-runner-vacuum-boundary | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut06-openmoc-runner-vacuum-boundary | openmoc-pincell-fuel-sigma-s | missed | 0.00470 | 0.00419 | 0.89221 |
| Mut06-openmoc-runner-vacuum-boundary | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut06-openmoc-runner-vacuum-boundary | openmoc-pincell-fuel-radius | missed | 0.00470 | 0.00497 | 1.05813 |
| Mut06-openmoc-runner-vacuum-boundary | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut06-openmoc-runner-vacuum-boundary | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut07-openmoc-adapter-nsf-inverse | openmoc-pincell-nu-sigma-f | detected | 1.13306 | 0.75516 | 0.66648 |
| Mut07-openmoc-adapter-nsf-inverse | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut07-openmoc-adapter-nsf-inverse | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut07-openmoc-adapter-nsf-inverse | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut07-openmoc-adapter-nsf-inverse | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut07-openmoc-adapter-nsf-inverse | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut07-openmoc-adapter-nsf-inverse | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut07-openmoc-adapter-nsf-inverse | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut07-openmoc-adapter-nsf-inverse | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut07-openmoc-adapter-nsf-inverse | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut07-openmoc-adapter-nsf-inverse | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut07-openmoc-adapter-nsf-inverse | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut07-openmoc-adapter-nsf-inverse | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut07-openmoc-adapter-nsf-inverse | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut07-openmoc-adapter-nsf-inverse | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut08-openmoc-adapter-nsf-square | openmoc-pincell-nu-sigma-f | missed | 1.13306 | 2.55018 | 2.25071 |
| Mut08-openmoc-adapter-nsf-square | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut08-openmoc-adapter-nsf-square | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut08-openmoc-adapter-nsf-square | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut08-openmoc-adapter-nsf-square | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut08-openmoc-adapter-nsf-square | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut08-openmoc-adapter-nsf-square | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut08-openmoc-adapter-nsf-square | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut08-openmoc-adapter-nsf-square | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut08-openmoc-adapter-nsf-square | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut08-openmoc-adapter-nsf-square | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut08-openmoc-adapter-nsf-square | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut08-openmoc-adapter-nsf-square | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut08-openmoc-adapter-nsf-square | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut08-openmoc-adapter-nsf-square | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut09-openmoc-adapter-nsf-moderator | openmoc-pincell-nu-sigma-f | detected | 1.13306 | 1.13306 | 1.00000 |
| Mut09-openmoc-adapter-nsf-moderator | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut09-openmoc-adapter-nsf-moderator | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut09-openmoc-adapter-nsf-moderator | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut09-openmoc-adapter-nsf-moderator | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut09-openmoc-adapter-nsf-moderator | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut09-openmoc-adapter-nsf-moderator | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut09-openmoc-adapter-nsf-moderator | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut09-openmoc-adapter-nsf-moderator | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut09-openmoc-adapter-nsf-moderator | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut09-openmoc-adapter-nsf-moderator | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut09-openmoc-adapter-nsf-moderator | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut09-openmoc-adapter-nsf-moderator | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut09-openmoc-adapter-nsf-moderator | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut09-openmoc-adapter-nsf-moderator | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut10-openmoc-adapter-nsf-identity | openmoc-pincell-nu-sigma-f | detected | 1.13306 | 1.13306 | 1.00000 |
| Mut10-openmoc-adapter-nsf-identity | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut10-openmoc-adapter-nsf-identity | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut10-openmoc-adapter-nsf-identity | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut10-openmoc-adapter-nsf-identity | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut10-openmoc-adapter-nsf-identity | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut10-openmoc-adapter-nsf-identity | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut10-openmoc-adapter-nsf-identity | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut10-openmoc-adapter-nsf-identity | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut10-openmoc-adapter-nsf-identity | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut10-openmoc-adapter-nsf-identity | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut10-openmoc-adapter-nsf-identity | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut10-openmoc-adapter-nsf-identity | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut10-openmoc-adapter-nsf-identity | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut10-openmoc-adapter-nsf-identity | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut11-openmoc-adapter-nsf-fast-only | openmoc-pincell-nu-sigma-f | detected | 1.13306 | 0.52376 | 0.46226 |
| Mut11-openmoc-adapter-nsf-fast-only | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut11-openmoc-adapter-nsf-fast-only | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut11-openmoc-adapter-nsf-fast-only | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut11-openmoc-adapter-nsf-fast-only | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut11-openmoc-adapter-nsf-fast-only | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut11-openmoc-adapter-nsf-fast-only | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut11-openmoc-adapter-nsf-fast-only | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut11-openmoc-adapter-nsf-fast-only | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut11-openmoc-adapter-nsf-fast-only | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut11-openmoc-adapter-nsf-fast-only | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut11-openmoc-adapter-nsf-fast-only | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut11-openmoc-adapter-nsf-fast-only | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut11-openmoc-adapter-nsf-fast-only | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut11-openmoc-adapter-nsf-fast-only | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmoc-pincell-sigma-a | detected | 1.13306 | 1.13306 | 1.00000 |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut12-openmoc-adapter-sa-no-sigt-update | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut13-openmoc-adapter-sa-inverse | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut13-openmoc-adapter-sa-inverse | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut13-openmoc-adapter-sa-inverse | openmoc-pincell-sigma-a | detected | 1.13306 | 1.51087 | 1.33344 |
| Mut13-openmoc-adapter-sa-inverse | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut13-openmoc-adapter-sa-inverse | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut13-openmoc-adapter-sa-inverse | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut13-openmoc-adapter-sa-inverse | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut13-openmoc-adapter-sa-inverse | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut13-openmoc-adapter-sa-inverse | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut13-openmoc-adapter-sa-inverse | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut13-openmoc-adapter-sa-inverse | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut13-openmoc-adapter-sa-inverse | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut13-openmoc-adapter-sa-inverse | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut13-openmoc-adapter-sa-inverse | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut13-openmoc-adapter-sa-inverse | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut14-openmoc-adapter-sa-moderator | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut14-openmoc-adapter-sa-moderator | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut14-openmoc-adapter-sa-moderator | openmoc-pincell-sigma-a | missed | 1.13306 | 0.47635 | 0.42041 |
| Mut14-openmoc-adapter-sa-moderator | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut14-openmoc-adapter-sa-moderator | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut14-openmoc-adapter-sa-moderator | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut14-openmoc-adapter-sa-moderator | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut14-openmoc-adapter-sa-moderator | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut14-openmoc-adapter-sa-moderator | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut14-openmoc-adapter-sa-moderator | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut14-openmoc-adapter-sa-moderator | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut14-openmoc-adapter-sa-moderator | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut14-openmoc-adapter-sa-moderator | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut14-openmoc-adapter-sa-moderator | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut14-openmoc-adapter-sa-moderator | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut17-openmc-runner-vacuum-boundary | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut17-openmc-runner-vacuum-boundary | openmc-pincell-nu-sigma-f | missed | 0.00491 | 0.00738 | 1.50153 |
| Mut17-openmc-runner-vacuum-boundary | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut17-openmc-runner-vacuum-boundary | openmc-pincell-sigma-a | detected | 0.00491 | 0.00486 | 0.98945 |
| Mut17-openmc-runner-vacuum-boundary | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut17-openmc-runner-vacuum-boundary | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut17-openmc-runner-vacuum-boundary | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut17-openmc-runner-vacuum-boundary | openmc-pincell-group-permute | missed | 0.00491 | 0.00489 | 0.99636 |
| Mut17-openmc-runner-vacuum-boundary | openmc-pincell-fuel-sigma-t | missed | 0.00491 | 0.00434 | 0.88440 |
| Mut17-openmc-runner-vacuum-boundary | openmc-pincell-moderator-sigma-a | detected | 0.00491 | 0.00496 | 1.00879 |
| Mut17-openmc-runner-vacuum-boundary | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut17-openmc-runner-vacuum-boundary | openmc-pincell-fuel-sigma-s | missed | 0.00491 | 0.00436 | 0.88847 |
| Mut17-openmc-runner-vacuum-boundary | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut17-openmc-runner-vacuum-boundary | openmc-pincell-fuel-radius | missed | 0.00491 | 0.00515 | 1.04906 |
| Mut17-openmc-runner-vacuum-boundary | openmc-pincell-particles-refine | detected | 0.00491 | 0.00489 | 0.99474 |
| Mut17-openmc-runner-vacuum-boundary | openmoc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut17-openmc-runner-vacuum-boundary | openmc-pincell-rotate-90 | detected | 0.00478 | 0.00485 | 1.01362 |
| Mut18-openmc-runner-batches-too-few | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut18-openmc-runner-batches-too-few | openmc-pincell-nu-sigma-f | missed | 1.09187 | 1.73009 | 1.58452 |
| Mut18-openmc-runner-batches-too-few | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut18-openmc-runner-batches-too-few | openmc-pincell-sigma-a | missed | 1.09187 | 0.86127 | 0.78881 |
| Mut18-openmc-runner-batches-too-few | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut18-openmc-runner-batches-too-few | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut18-openmc-runner-batches-too-few | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut18-openmc-runner-batches-too-few | openmc-pincell-group-permute | detected | 1.09187 | 1.22741 | 1.12414 |
| Mut18-openmc-runner-batches-too-few | openmc-pincell-fuel-sigma-t | missed | 1.09187 | 0.11146 | 0.10208 |
| Mut18-openmc-runner-batches-too-few | openmc-pincell-moderator-sigma-a | detected | 1.09187 | 0.99763 | 0.91370 |
| Mut18-openmc-runner-batches-too-few | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut18-openmc-runner-batches-too-few | openmc-pincell-fuel-sigma-s | detected | 1.09187 | 1.17034 | 1.07187 |
| Mut18-openmc-runner-batches-too-few | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut18-openmc-runner-batches-too-few | openmc-pincell-fuel-radius | detected | 1.09187 | 1.13717 | 1.04149 |
| Mut18-openmc-runner-batches-too-few | openmc-pincell-particles-refine | detected | 1.09187 | 1.09187 | 1.00000 |
| Mut18-openmc-runner-batches-too-few | openmoc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut18-openmc-runner-batches-too-few | openmc-pincell-rotate-90 | missed | 1.19271 | 1.18848 | 0.99645 |
| Mut19-openmc-runner-hardcode-keff | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut19-openmc-runner-hardcode-keff | openmc-pincell-nu-sigma-f | detected | 1.00000 | 1.00000 | 1.00000 |
| Mut19-openmc-runner-hardcode-keff | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut19-openmc-runner-hardcode-keff | openmc-pincell-sigma-a | detected | 1.00000 | 1.00000 | 1.00000 |
| Mut19-openmc-runner-hardcode-keff | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut19-openmc-runner-hardcode-keff | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut19-openmc-runner-hardcode-keff | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut19-openmc-runner-hardcode-keff | openmc-pincell-group-permute | missed | 1.00000 | 1.00000 | 1.00000 |
| Mut19-openmc-runner-hardcode-keff | openmc-pincell-fuel-sigma-t | detected | 1.00000 | 1.00000 | 1.00000 |
| Mut19-openmc-runner-hardcode-keff | openmc-pincell-moderator-sigma-a | detected | 1.00000 | 1.00000 | 1.00000 |
| Mut19-openmc-runner-hardcode-keff | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut19-openmc-runner-hardcode-keff | openmc-pincell-fuel-sigma-s | detected | 1.00000 | 1.00000 | 1.00000 |
| Mut19-openmc-runner-hardcode-keff | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut19-openmc-runner-hardcode-keff | openmc-pincell-fuel-radius | detected | 1.00000 | 1.00000 | 1.00000 |
| Mut19-openmc-runner-hardcode-keff | openmc-pincell-particles-refine | missed | 1.00000 | 1.00000 | 1.00000 |
| Mut19-openmc-runner-hardcode-keff | openmoc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut19-openmc-runner-hardcode-keff | openmc-pincell-rotate-90 | missed | 1.00000 | 1.00000 | 1.00000 |
| Mut20-openmc-runner-chi-swap-groups | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut20-openmc-runner-chi-swap-groups | openmc-pincell-nu-sigma-f | missed | 1.27937 | 1.91775 | 1.49898 |
| Mut20-openmc-runner-chi-swap-groups | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut20-openmc-runner-chi-swap-groups | openmc-pincell-sigma-a | missed | 1.27937 | 0.96310 | 0.75280 |
| Mut20-openmc-runner-chi-swap-groups | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut20-openmc-runner-chi-swap-groups | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut20-openmc-runner-chi-swap-groups | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut20-openmc-runner-chi-swap-groups | openmc-pincell-group-permute | missed | 1.27937 | 1.27735 | 0.99843 |
| Mut20-openmc-runner-chi-swap-groups | openmc-pincell-fuel-sigma-t | missed | 1.27937 | 0.29025 | 0.22687 |
| Mut20-openmc-runner-chi-swap-groups | openmc-pincell-moderator-sigma-a | missed | 1.27937 | 1.09434 | 0.85538 |
| Mut20-openmc-runner-chi-swap-groups | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut20-openmc-runner-chi-swap-groups | openmc-pincell-fuel-sigma-s | detected | 1.27937 | 1.27837 | 0.99922 |
| Mut20-openmc-runner-chi-swap-groups | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut20-openmc-runner-chi-swap-groups | openmc-pincell-fuel-radius | missed | 1.27937 | 1.33994 | 1.04735 |
| Mut20-openmc-runner-chi-swap-groups | openmc-pincell-particles-refine | missed | 1.27937 | 1.27814 | 0.99905 |
| Mut20-openmc-runner-chi-swap-groups | openmoc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut20-openmc-runner-chi-swap-groups | openmc-pincell-rotate-90 | missed | 1.31484 | 1.31881 | 1.00302 |
| Mut21-openmc-runner-fission-zero | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut21-openmc-runner-fission-zero | openmc-pincell-nu-sigma-f | missed | 1.12450 | 1.68996 | 1.50286 |
| Mut21-openmc-runner-fission-zero | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut21-openmc-runner-fission-zero | openmc-pincell-sigma-a | missed | 1.12450 | 0.80272 | 0.71384 |
| Mut21-openmc-runner-fission-zero | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut21-openmc-runner-fission-zero | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut21-openmc-runner-fission-zero | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut21-openmc-runner-fission-zero | openmc-pincell-group-permute | missed | 1.12450 | 1.12601 | 1.00134 |
| Mut21-openmc-runner-fission-zero | openmc-pincell-fuel-sigma-t | missed | 1.12450 | 0.11292 | 0.10042 |
| Mut21-openmc-runner-fission-zero | openmc-pincell-moderator-sigma-a | missed | 1.12450 | 0.96831 | 0.86110 |
| Mut21-openmc-runner-fission-zero | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut21-openmc-runner-fission-zero | openmc-pincell-fuel-sigma-s | missed | 1.12450 | 1.09326 | 0.97222 |
| Mut21-openmc-runner-fission-zero | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut21-openmc-runner-fission-zero | openmc-pincell-fuel-radius | missed | 1.12450 | 1.17086 | 1.04123 |
| Mut21-openmc-runner-fission-zero | openmc-pincell-particles-refine | _error_ |  |  |  |
| Mut21-openmc-runner-fission-zero | openmoc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut21-openmc-runner-fission-zero | openmc-pincell-rotate-90 | missed | 1.15438 | 1.15354 | 0.99927 |
| Mut22-openmc-adapter-nsf-inverse | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut22-openmc-adapter-nsf-inverse | openmc-pincell-nu-sigma-f | detected | 1.12450 | 0.75033 | 0.66726 |
| Mut22-openmc-adapter-nsf-inverse | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut22-openmc-adapter-nsf-inverse | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut22-openmc-adapter-nsf-inverse | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut22-openmc-adapter-nsf-inverse | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut22-openmc-adapter-nsf-inverse | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut22-openmc-adapter-nsf-inverse | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut22-openmc-adapter-nsf-inverse | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut22-openmc-adapter-nsf-inverse | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut22-openmc-adapter-nsf-inverse | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut22-openmc-adapter-nsf-inverse | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut22-openmc-adapter-nsf-inverse | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut22-openmc-adapter-nsf-inverse | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut22-openmc-adapter-nsf-inverse | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut23-openmc-adapter-nsf-square | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut23-openmc-adapter-nsf-square | openmc-pincell-nu-sigma-f | missed | 1.12450 | 2.53531 | 2.25461 |
| Mut23-openmc-adapter-nsf-square | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut23-openmc-adapter-nsf-square | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut23-openmc-adapter-nsf-square | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut23-openmc-adapter-nsf-square | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut23-openmc-adapter-nsf-square | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut23-openmc-adapter-nsf-square | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut23-openmc-adapter-nsf-square | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut23-openmc-adapter-nsf-square | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut23-openmc-adapter-nsf-square | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut23-openmc-adapter-nsf-square | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut23-openmc-adapter-nsf-square | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut23-openmc-adapter-nsf-square | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut23-openmc-adapter-nsf-square | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut24-openmc-adapter-nsf-moderator | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut24-openmc-adapter-nsf-moderator | openmc-pincell-nu-sigma-f | detected | 1.12450 | 1.12450 | 1.00000 |
| Mut24-openmc-adapter-nsf-moderator | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut24-openmc-adapter-nsf-moderator | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut24-openmc-adapter-nsf-moderator | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut24-openmc-adapter-nsf-moderator | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut24-openmc-adapter-nsf-moderator | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut24-openmc-adapter-nsf-moderator | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut24-openmc-adapter-nsf-moderator | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut24-openmc-adapter-nsf-moderator | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut24-openmc-adapter-nsf-moderator | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut24-openmc-adapter-nsf-moderator | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut24-openmc-adapter-nsf-moderator | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut24-openmc-adapter-nsf-moderator | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut24-openmc-adapter-nsf-moderator | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut25-openmc-adapter-nsf-identity | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut25-openmc-adapter-nsf-identity | openmc-pincell-nu-sigma-f | detected | 1.12450 | 1.12450 | 1.00000 |
| Mut25-openmc-adapter-nsf-identity | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut25-openmc-adapter-nsf-identity | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut25-openmc-adapter-nsf-identity | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut25-openmc-adapter-nsf-identity | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut25-openmc-adapter-nsf-identity | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut25-openmc-adapter-nsf-identity | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut25-openmc-adapter-nsf-identity | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut25-openmc-adapter-nsf-identity | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut25-openmc-adapter-nsf-identity | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut25-openmc-adapter-nsf-identity | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut25-openmc-adapter-nsf-identity | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut25-openmc-adapter-nsf-identity | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut25-openmc-adapter-nsf-identity | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut27-openmc-adapter-sa-inverse | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut27-openmc-adapter-sa-inverse | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut27-openmc-adapter-sa-inverse | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut27-openmc-adapter-sa-inverse | openmc-pincell-sigma-a | detected | 1.12450 | 1.49882 | 1.33287 |
| Mut27-openmc-adapter-sa-inverse | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut27-openmc-adapter-sa-inverse | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut27-openmc-adapter-sa-inverse | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut27-openmc-adapter-sa-inverse | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut27-openmc-adapter-sa-inverse | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut27-openmc-adapter-sa-inverse | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut27-openmc-adapter-sa-inverse | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut27-openmc-adapter-sa-inverse | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut27-openmc-adapter-sa-inverse | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut27-openmc-adapter-sa-inverse | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut27-openmc-adapter-sa-inverse | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut28-openmoc-runner-chi-fast-only | openmoc-pincell-nu-sigma-f | missed | 1.13306 | 1.69990 | 1.50028 |
| Mut28-openmoc-runner-chi-fast-only | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut28-openmoc-runner-chi-fast-only | openmoc-pincell-sigma-a | missed | 1.13306 | 0.80690 | 0.71215 |
| Mut28-openmoc-runner-chi-fast-only | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut28-openmoc-runner-chi-fast-only | openmoc-pincell-group-permute | detected | 1.13306 | 1.27800 | 1.12792 |
| Mut28-openmoc-runner-chi-fast-only | openmoc-pincell-fuel-sigma-t | missed | 1.13306 | 0.11127 | 0.09821 |
| Mut28-openmoc-runner-chi-fast-only | openmoc-pincell-moderator-sigma-a | missed | 1.13306 | 0.47635 | 0.42041 |
| Mut28-openmoc-runner-chi-fast-only | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut28-openmoc-runner-chi-fast-only | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut28-openmoc-runner-chi-fast-only | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut28-openmoc-runner-chi-fast-only | openmoc-pincell-fuel-sigma-s | missed | 1.13306 | 1.09185 | 0.96363 |
| Mut28-openmoc-runner-chi-fast-only | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut28-openmoc-runner-chi-fast-only | openmoc-pincell-fuel-radius | missed | 1.13306 | 1.17476 | 1.03681 |
| Mut28-openmoc-runner-chi-fast-only | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut28-openmoc-runner-chi-fast-only | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmoc-pincell-moderator-sigma-a | detected | 1.13306 | 1.13306 | 1.00000 |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmoc-pincell-group-permute | detected | 1.13306 | 0.53806 | 0.47487 |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut31-openmoc-adapter-group-permute-fuel-only | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmoc-pincell-fuel-sigma-s | detected | 1.13306 | 1.13306 | 1.00000 |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmoc-pincell-fuel-radius | detected | 1.13306 | 1.09021 | 0.96218 |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut33-openmoc-adapter-fuel-radius-shrink | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut34-openmc-adapter-particles-no-op | openmc-pincell-particles-refine | detected | 1.12450 | 1.12450 | 1.00000 |
| Mut35-openmc-runner-chi-fast-only | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut35-openmc-runner-chi-fast-only | openmc-pincell-nu-sigma-f | missed | 1.12450 | 1.68996 | 1.50286 |
| Mut35-openmc-runner-chi-fast-only | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut35-openmc-runner-chi-fast-only | openmc-pincell-sigma-a | missed | 1.12450 | 0.80272 | 0.71384 |
| Mut35-openmc-runner-chi-fast-only | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut35-openmc-runner-chi-fast-only | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut35-openmc-runner-chi-fast-only | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut35-openmc-runner-chi-fast-only | openmc-pincell-group-permute | detected | 1.12450 | 1.27735 | 1.13593 |
| Mut35-openmc-runner-chi-fast-only | openmc-pincell-fuel-sigma-t | missed | 1.12450 | 0.11292 | 0.10042 |
| Mut35-openmc-runner-chi-fast-only | openmc-pincell-moderator-sigma-a | missed | 1.12450 | 0.96831 | 0.86110 |
| Mut35-openmc-runner-chi-fast-only | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut35-openmc-runner-chi-fast-only | openmc-pincell-fuel-sigma-s | missed | 1.12450 | 1.09326 | 0.97222 |
| Mut35-openmc-runner-chi-fast-only | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut35-openmc-runner-chi-fast-only | openmc-pincell-fuel-radius | missed | 1.12450 | 1.17086 | 1.04123 |
| Mut35-openmc-runner-chi-fast-only | openmc-pincell-particles-refine | missed | 1.12450 | 1.12477 | 1.00024 |
| Mut36-openmc-adapter-group-permute-fuel-only | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut36-openmc-adapter-group-permute-fuel-only | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut36-openmc-adapter-group-permute-fuel-only | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut36-openmc-adapter-group-permute-fuel-only | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut36-openmc-adapter-group-permute-fuel-only | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut36-openmc-adapter-group-permute-fuel-only | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut36-openmc-adapter-group-permute-fuel-only | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut36-openmc-adapter-group-permute-fuel-only | openmc-pincell-group-permute | detected | 1.12450 | 0.53678 | 0.47735 |
| Mut36-openmc-adapter-group-permute-fuel-only | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut36-openmc-adapter-group-permute-fuel-only | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut36-openmc-adapter-group-permute-fuel-only | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut36-openmc-adapter-group-permute-fuel-only | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut36-openmc-adapter-group-permute-fuel-only | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut36-openmc-adapter-group-permute-fuel-only | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut36-openmc-adapter-group-permute-fuel-only | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmc-pincell-fuel-sigma-s | detected | 1.12450 | 1.12450 | 1.00000 |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmoc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmoc-pincell-mirror-x | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmc-pincell-mirror-x | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmoc-pincell-mirror-y | _not-affected_ |  |  |  |
| Mut37-openmc-adapter-fuel-sigma-s-identity | openmc-pincell-mirror-y | _not-affected_ |  |  |  |
| Mut38-openmc-adapter-fuel-radius-shrink | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut38-openmc-adapter-fuel-radius-shrink | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut38-openmc-adapter-fuel-radius-shrink | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut38-openmc-adapter-fuel-radius-shrink | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut38-openmc-adapter-fuel-radius-shrink | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut38-openmc-adapter-fuel-radius-shrink | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut38-openmc-adapter-fuel-radius-shrink | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut38-openmc-adapter-fuel-radius-shrink | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut38-openmc-adapter-fuel-radius-shrink | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut38-openmc-adapter-fuel-radius-shrink | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut38-openmc-adapter-fuel-radius-shrink | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut38-openmc-adapter-fuel-radius-shrink | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut38-openmc-adapter-fuel-radius-shrink | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut38-openmc-adapter-fuel-radius-shrink | openmc-pincell-fuel-radius | detected | 1.12450 | 1.08116 | 0.96146 |
| Mut38-openmc-adapter-fuel-radius-shrink | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut39-openmoc-runner-hardcode-y-from-x | openmoc-pincell-nu-sigma-f | missed | 1.13306 | 1.69990 | 1.50028 |
| Mut39-openmoc-runner-hardcode-y-from-x | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut39-openmoc-runner-hardcode-y-from-x | openmoc-pincell-sigma-a | missed | 1.13306 | 0.80690 | 0.71215 |
| Mut39-openmoc-runner-hardcode-y-from-x | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut39-openmoc-runner-hardcode-y-from-x | openmoc-pincell-group-permute | missed | 1.13306 | 1.13306 | 1.00000 |
| Mut39-openmoc-runner-hardcode-y-from-x | openmoc-pincell-fuel-sigma-t | missed | 1.13306 | 0.11127 | 0.09821 |
| Mut39-openmoc-runner-hardcode-y-from-x | openmoc-pincell-moderator-sigma-a | missed | 1.13306 | 0.47635 | 0.42041 |
| Mut39-openmoc-runner-hardcode-y-from-x | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut39-openmoc-runner-hardcode-y-from-x | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut39-openmoc-runner-hardcode-y-from-x | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut39-openmoc-runner-hardcode-y-from-x | openmoc-pincell-fuel-sigma-s | missed | 1.13306 | 1.09185 | 0.96363 |
| Mut39-openmoc-runner-hardcode-y-from-x | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut39-openmoc-runner-hardcode-y-from-x | openmoc-pincell-fuel-radius | missed | 1.13306 | 1.17476 | 1.03681 |
| Mut39-openmoc-runner-hardcode-y-from-x | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut39-openmoc-runner-hardcode-y-from-x | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut39-openmoc-runner-hardcode-y-from-x | openmoc-pincell-rotate-90 | detected | 1.33152 | 0.53551 | 0.40218 |
| Mut39-openmoc-runner-hardcode-y-from-x | openmc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut40-openmc-runner-hardcode-y-from-x | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut40-openmc-runner-hardcode-y-from-x | openmc-pincell-nu-sigma-f | missed | 1.12450 | 1.68996 | 1.50286 |
| Mut40-openmc-runner-hardcode-y-from-x | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut40-openmc-runner-hardcode-y-from-x | openmc-pincell-sigma-a | missed | 1.12450 | 0.80272 | 0.71384 |
| Mut40-openmc-runner-hardcode-y-from-x | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut40-openmc-runner-hardcode-y-from-x | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut40-openmc-runner-hardcode-y-from-x | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut40-openmc-runner-hardcode-y-from-x | openmc-pincell-group-permute | missed | 1.12450 | 1.12601 | 1.00134 |
| Mut40-openmc-runner-hardcode-y-from-x | openmc-pincell-fuel-sigma-t | missed | 1.12450 | 0.11292 | 0.10042 |
| Mut40-openmc-runner-hardcode-y-from-x | openmc-pincell-moderator-sigma-a | missed | 1.12450 | 0.96831 | 0.86110 |
| Mut40-openmc-runner-hardcode-y-from-x | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut40-openmc-runner-hardcode-y-from-x | openmc-pincell-fuel-sigma-s | missed | 1.12450 | 1.09326 | 0.97222 |
| Mut40-openmc-runner-hardcode-y-from-x | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut40-openmc-runner-hardcode-y-from-x | openmc-pincell-fuel-radius | missed | 1.12450 | 1.17086 | 1.04123 |
| Mut40-openmc-runner-hardcode-y-from-x | openmc-pincell-particles-refine | missed | 1.12450 | 1.12477 | 1.00024 |
| Mut40-openmc-runner-hardcode-y-from-x | openmoc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut40-openmc-runner-hardcode-y-from-x | openmc-pincell-rotate-90 | detected | 1.31933 | 0.95812 | 0.72621 |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmoc-pincell-nu-sigma-f | missed | 1.13306 | 1.69990 | 1.50028 |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmoc-pincell-sigma-a | missed | 1.13306 | 0.80690 | 0.71215 |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmoc-pincell-group-permute | missed | 1.13306 | 1.13306 | 1.00000 |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmoc-pincell-fuel-sigma-t | missed | 1.13306 | 0.11127 | 0.09821 |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmoc-pincell-moderator-sigma-a | missed | 1.13306 | 0.47635 | 0.42041 |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmoc-pincell-fuel-sigma-s | missed | 1.13306 | 1.09185 | 0.96363 |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmoc-pincell-fuel-radius | missed | 1.13306 | 1.17476 | 1.03681 |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmoc-pincell-rotate-90 | missed | 1.15950 | 1.15949 | 0.99999 |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmoc-pincell-mirror-x | detected | 1.10371 | 1.10270 | 0.99908 |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmc-pincell-mirror-x | _not-affected_ |  |  |  |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmoc-pincell-mirror-y | missed | 1.10371 | 1.10371 | 1.00000 |
| Mut41-openmoc-runner-clamp-y-offset-positive | openmc-pincell-mirror-y | _not-affected_ |  |  |  |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmoc-pincell-nu-sigma-f | missed | 1.13306 | 1.69990 | 1.50028 |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmoc-pincell-sigma-a | missed | 1.13306 | 0.80690 | 0.71215 |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmoc-pincell-group-permute | missed | 1.13306 | 1.13306 | 1.00000 |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmoc-pincell-fuel-sigma-t | missed | 1.13306 | 0.11127 | 0.09821 |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmoc-pincell-moderator-sigma-a | missed | 1.13306 | 0.47635 | 0.42041 |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmoc-pincell-fuel-sigma-s | missed | 1.13306 | 1.09185 | 0.96363 |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmoc-pincell-fuel-radius | missed | 1.13306 | 1.17476 | 1.03681 |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmoc-pincell-rotate-90 | missed | 1.15950 | 1.15949 | 0.99999 |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmoc-pincell-mirror-x | missed | 1.10271 | 1.10270 | 0.99999 |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmc-pincell-mirror-x | _not-affected_ |  |  |  |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmoc-pincell-mirror-y | detected | 1.10271 | 1.10484 | 1.00193 |
| Mut42-openmoc-runner-clamp-x-offset-positive | openmc-pincell-mirror-y | _not-affected_ |  |  |  |
| Mut43-openmc-runner-clamp-y-offset-positive | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut43-openmc-runner-clamp-y-offset-positive | openmc-pincell-nu-sigma-f | missed | 1.12450 | 1.68996 | 1.50286 |
| Mut43-openmc-runner-clamp-y-offset-positive | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut43-openmc-runner-clamp-y-offset-positive | openmc-pincell-sigma-a | missed | 1.12450 | 0.80272 | 0.71384 |
| Mut43-openmc-runner-clamp-y-offset-positive | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut43-openmc-runner-clamp-y-offset-positive | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut43-openmc-runner-clamp-y-offset-positive | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut43-openmc-runner-clamp-y-offset-positive | openmc-pincell-group-permute | missed | 1.12450 | 1.12601 | 1.00134 |
| Mut43-openmc-runner-clamp-y-offset-positive | openmc-pincell-fuel-sigma-t | missed | 1.12450 | 0.11292 | 0.10042 |
| Mut43-openmc-runner-clamp-y-offset-positive | openmc-pincell-moderator-sigma-a | missed | 1.12450 | 0.96831 | 0.86110 |
| Mut43-openmc-runner-clamp-y-offset-positive | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut43-openmc-runner-clamp-y-offset-positive | openmc-pincell-fuel-sigma-s | missed | 1.12450 | 1.09326 | 0.97222 |
| Mut43-openmc-runner-clamp-y-offset-positive | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut43-openmc-runner-clamp-y-offset-positive | openmc-pincell-fuel-radius | missed | 1.12450 | 1.17086 | 1.04123 |
| Mut43-openmc-runner-clamp-y-offset-positive | openmc-pincell-particles-refine | missed | 1.12450 | 1.12477 | 1.00024 |
| Mut43-openmc-runner-clamp-y-offset-positive | openmoc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut43-openmc-runner-clamp-y-offset-positive | openmc-pincell-rotate-90 | missed | 1.15438 | 1.15354 | 0.99927 |
| Mut43-openmc-runner-clamp-y-offset-positive | openmoc-pincell-mirror-x | _not-affected_ |  |  |  |
| Mut43-openmc-runner-clamp-y-offset-positive | openmc-pincell-mirror-x | missed | 1.09673 | 1.09808 | 1.00123 |
| Mut43-openmc-runner-clamp-y-offset-positive | openmoc-pincell-mirror-y | _not-affected_ |  |  |  |
| Mut43-openmc-runner-clamp-y-offset-positive | openmc-pincell-mirror-y | missed | 1.09673 | 1.09575 | 0.99910 |
| Mut44-openmc-runner-clamp-x-offset-positive | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut44-openmc-runner-clamp-x-offset-positive | openmc-pincell-nu-sigma-f | missed | 1.12450 | 1.68996 | 1.50286 |
| Mut44-openmc-runner-clamp-x-offset-positive | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut44-openmc-runner-clamp-x-offset-positive | openmc-pincell-sigma-a | missed | 1.12450 | 0.80272 | 0.71384 |
| Mut44-openmc-runner-clamp-x-offset-positive | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut44-openmc-runner-clamp-x-offset-positive | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut44-openmc-runner-clamp-x-offset-positive | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut44-openmc-runner-clamp-x-offset-positive | openmc-pincell-group-permute | missed | 1.12450 | 1.12601 | 1.00134 |
| Mut44-openmc-runner-clamp-x-offset-positive | openmc-pincell-fuel-sigma-t | missed | 1.12450 | 0.11292 | 0.10042 |
| Mut44-openmc-runner-clamp-x-offset-positive | openmc-pincell-moderator-sigma-a | missed | 1.12450 | 0.96831 | 0.86110 |
| Mut44-openmc-runner-clamp-x-offset-positive | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut44-openmc-runner-clamp-x-offset-positive | openmc-pincell-fuel-sigma-s | missed | 1.12450 | 1.09326 | 0.97222 |
| Mut44-openmc-runner-clamp-x-offset-positive | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut44-openmc-runner-clamp-x-offset-positive | openmc-pincell-fuel-radius | missed | 1.12450 | 1.17086 | 1.04123 |
| Mut44-openmc-runner-clamp-x-offset-positive | openmc-pincell-particles-refine | _error_ |  |  |  |
| Mut44-openmc-runner-clamp-x-offset-positive | openmoc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut44-openmc-runner-clamp-x-offset-positive | openmc-pincell-rotate-90 | missed | 1.15438 | 1.15354 | 0.99927 |
| Mut44-openmc-runner-clamp-x-offset-positive | openmoc-pincell-mirror-x | _not-affected_ |  |  |  |
| Mut44-openmc-runner-clamp-x-offset-positive | openmc-pincell-mirror-x | missed | 1.09602 | 1.09808 | 1.00188 |
| Mut44-openmc-runner-clamp-x-offset-positive | openmoc-pincell-mirror-y | _not-affected_ |  |  |  |
| Mut44-openmc-runner-clamp-x-offset-positive | openmc-pincell-mirror-y | missed | 1.09602 | 1.09443 | 0.99855 |
| Mut45-openmoc-runner-ignore-temperature | openmoc-pincell-nu-sigma-f | missed | 1.13306 | 1.69990 | 1.50028 |
| Mut45-openmoc-runner-ignore-temperature | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut45-openmoc-runner-ignore-temperature | openmoc-pincell-sigma-a | missed | 1.13306 | 0.80690 | 0.71215 |
| Mut45-openmoc-runner-ignore-temperature | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut45-openmoc-runner-ignore-temperature | openmoc-pincell-group-permute | missed | 1.13306 | 1.13306 | 1.00000 |
| Mut45-openmoc-runner-ignore-temperature | openmoc-pincell-fuel-sigma-t | missed | 1.13306 | 0.11127 | 0.09821 |
| Mut45-openmoc-runner-ignore-temperature | openmoc-pincell-moderator-sigma-a | missed | 1.13306 | 0.47635 | 0.42041 |
| Mut45-openmoc-runner-ignore-temperature | openmc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut45-openmoc-runner-ignore-temperature | openmc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut45-openmoc-runner-ignore-temperature | openmc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut45-openmoc-runner-ignore-temperature | openmoc-pincell-fuel-sigma-s | missed | 1.13306 | 1.09185 | 0.96363 |
| Mut45-openmoc-runner-ignore-temperature | openmc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut45-openmoc-runner-ignore-temperature | openmoc-pincell-fuel-radius | missed | 1.13306 | 1.17476 | 1.03681 |
| Mut45-openmoc-runner-ignore-temperature | openmc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut45-openmoc-runner-ignore-temperature | openmc-pincell-particles-refine | _not-affected_ |  |  |  |
| Mut45-openmoc-runner-ignore-temperature | openmoc-pincell-rotate-90 | missed | 1.15950 | 1.15949 | 0.99999 |
| Mut45-openmoc-runner-ignore-temperature | openmc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut45-openmoc-runner-ignore-temperature | openmoc-pincell-mirror-x | missed | 1.10271 | 1.10270 | 0.99999 |
| Mut45-openmoc-runner-ignore-temperature | openmc-pincell-mirror-x | _not-affected_ |  |  |  |
| Mut45-openmoc-runner-ignore-temperature | openmoc-pincell-mirror-y | missed | 1.10271 | 1.10271 | 1.00000 |
| Mut45-openmoc-runner-ignore-temperature | openmc-pincell-mirror-y | _not-affected_ |  |  |  |
| Mut45-openmoc-runner-ignore-temperature | openmoc-pincell-fuel-temperature | detected | 1.13306 | 1.13306 | 1.00000 |
| Mut45-openmoc-runner-ignore-temperature | openmc-pincell-fuel-temperature | _not-affected_ |  |  |  |
| Mut46-openmc-runner-ignore-temperature | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| Mut46-openmc-runner-ignore-temperature | openmc-pincell-nu-sigma-f | missed | 1.12450 | 1.68996 | 1.50286 |
| Mut46-openmc-runner-ignore-temperature | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| Mut46-openmc-runner-ignore-temperature | openmc-pincell-sigma-a | missed | 1.12450 | 0.80272 | 0.71384 |
| Mut46-openmc-runner-ignore-temperature | openmoc-pincell-group-permute | _not-affected_ |  |  |  |
| Mut46-openmc-runner-ignore-temperature | openmoc-pincell-fuel-sigma-t | _not-affected_ |  |  |  |
| Mut46-openmc-runner-ignore-temperature | openmoc-pincell-moderator-sigma-a | _not-affected_ |  |  |  |
| Mut46-openmc-runner-ignore-temperature | openmc-pincell-group-permute | missed | 1.12450 | 1.12601 | 1.00134 |
| Mut46-openmc-runner-ignore-temperature | openmc-pincell-fuel-sigma-t | missed | 1.12450 | 0.11292 | 0.10042 |
| Mut46-openmc-runner-ignore-temperature | openmc-pincell-moderator-sigma-a | missed | 1.12450 | 0.96831 | 0.86110 |
| Mut46-openmc-runner-ignore-temperature | openmoc-pincell-fuel-sigma-s | _not-affected_ |  |  |  |
| Mut46-openmc-runner-ignore-temperature | openmc-pincell-fuel-sigma-s | missed | 1.12450 | 1.09326 | 0.97222 |
| Mut46-openmc-runner-ignore-temperature | openmoc-pincell-fuel-radius | _not-affected_ |  |  |  |
| Mut46-openmc-runner-ignore-temperature | openmc-pincell-fuel-radius | missed | 1.12450 | 1.17086 | 1.04123 |
| Mut46-openmc-runner-ignore-temperature | openmc-pincell-particles-refine | missed | 1.12450 | 1.12477 | 1.00024 |
| Mut46-openmc-runner-ignore-temperature | openmoc-pincell-rotate-90 | _not-affected_ |  |  |  |
| Mut46-openmc-runner-ignore-temperature | openmc-pincell-rotate-90 | missed | 1.15438 | 1.15354 | 0.99927 |
| Mut46-openmc-runner-ignore-temperature | openmoc-pincell-mirror-x | _not-affected_ |  |  |  |
| Mut46-openmc-runner-ignore-temperature | openmc-pincell-mirror-x | missed | 1.09602 | 1.09808 | 1.00188 |
| Mut46-openmc-runner-ignore-temperature | openmoc-pincell-mirror-y | _not-affected_ |  |  |  |
| Mut46-openmc-runner-ignore-temperature | openmc-pincell-mirror-y | missed | 1.09602 | 1.09555 | 0.99958 |
| Mut46-openmc-runner-ignore-temperature | openmoc-pincell-fuel-temperature | _not-affected_ |  |  |  |
| Mut46-openmc-runner-ignore-temperature | openmc-pincell-fuel-temperature | detected | 1.12450 | 1.12450 | 1.00000 |

## Cross-solver agreement (Cohen's κ)

κ measured on matched-pair mutants where the same conceptual fault is applied to both solvers' files (see `mutation-catalogue.md` matched-pair index). Each row: did the OpenMOC scenario detect the OpenMOC mutant; did the OpenMC scenario detect its OpenMC twin.


### ScaleNuSigmaF pairs

Pairs evaluated: 4 / 4. Cohen's κ = **1.000** (almost perfect).


| OpenMOC mutant | OpenMC mutant | OpenMOC outcome | OpenMC outcome |
|---|---|---|---|
| Mut07-openmoc-adapter-nsf-inverse | Mut22-openmc-adapter-nsf-inverse | detected | detected |
| Mut08-openmoc-adapter-nsf-square | Mut23-openmc-adapter-nsf-square | missed | missed |
| Mut09-openmoc-adapter-nsf-moderator | Mut24-openmc-adapter-nsf-moderator | detected | detected |
| Mut10-openmoc-adapter-nsf-identity | Mut25-openmc-adapter-nsf-identity | detected | detected |


### ScaleFuelSigmaA pairs

Pairs evaluated: 1 / 2. Cohen's κ = **1.000** (almost perfect).


| OpenMOC mutant | OpenMC mutant | OpenMOC outcome | OpenMC outcome |
|---|---|---|---|
| Mut13-openmoc-adapter-sa-inverse | Mut27-openmc-adapter-sa-inverse | detected | detected |


### Runner-level pairs (chi/boundary) — NuSigmaF scenarios

Pairs evaluated: 2 / 3. Cohen's κ = **1.000** (almost perfect).


| OpenMOC mutant | OpenMC mutant | OpenMOC outcome | OpenMC outcome |
|---|---|---|---|
| Mut05-openmoc-runner-chi-swap-groups | Mut20-openmc-runner-chi-swap-groups | missed | missed |
| Mut06-openmoc-runner-vacuum-boundary | Mut17-openmc-runner-vacuum-boundary | missed | missed |


### Phase-2 MR04 group-permute (chi-fast-only runner)

Pairs evaluated: 1 / 1. Cohen's κ = **1.000** (almost perfect).


| OpenMOC mutant | OpenMC mutant | OpenMOC outcome | OpenMC outcome |
|---|---|---|---|
| Mut28-openmoc-runner-chi-fast-only | Mut35-openmc-runner-chi-fast-only | detected | detected |


### Phase-2 MR04 group-permute (fuel-only adapter)

Pairs evaluated: 1 / 1. Cohen's κ = **1.000** (almost perfect).


| OpenMOC mutant | OpenMC mutant | OpenMOC outcome | OpenMC outcome |
|---|---|---|---|
| Mut31-openmoc-adapter-group-permute-fuel-only | Mut36-openmc-adapter-group-permute-fuel-only | detected | detected |


### Phase-2 MR06 fuel-sigma-s identity adapter

Pairs evaluated: 1 / 1. Cohen's κ = **1.000** (almost perfect).


| OpenMOC mutant | OpenMC mutant | OpenMOC outcome | OpenMC outcome |
|---|---|---|---|
| Mut32-openmoc-adapter-fuel-sigma-s-identity | Mut37-openmc-adapter-fuel-sigma-s-identity | detected | detected |


### Phase-2 MR08 fuel-radius direction inversion

Pairs evaluated: 1 / 1. Cohen's κ = **1.000** (almost perfect).


| OpenMOC mutant | OpenMC mutant | OpenMOC outcome | OpenMC outcome |
|---|---|---|---|
| Mut33-openmoc-adapter-fuel-radius-shrink | Mut38-openmc-adapter-fuel-radius-shrink | detected | detected |


### Phase-2 MR01 Rotate90 (hardcode-y-from-x)

Pairs evaluated: 1 / 1. Cohen's κ = **1.000** (almost perfect).


| OpenMOC mutant | OpenMC mutant | OpenMOC outcome | OpenMC outcome |
|---|---|---|---|
| Mut39-openmoc-runner-hardcode-y-from-x | Mut40-openmc-runner-hardcode-y-from-x | detected | detected |


### Phase-2 MR02 MirrorX (clamp-y-offset-positive)

Pairs evaluated: 1 / 1. Cohen's κ = **0.000** (slight).


| OpenMOC mutant | OpenMC mutant | OpenMOC outcome | OpenMC outcome |
|---|---|---|---|
| Mut41-openmoc-runner-clamp-y-offset-positive | Mut43-openmc-runner-clamp-y-offset-positive | detected | missed |


### Phase-2 MR03 MirrorY (clamp-x-offset-positive)

Pairs evaluated: 1 / 1. Cohen's κ = **0.000** (slight).


| OpenMOC mutant | OpenMC mutant | OpenMOC outcome | OpenMC outcome |
|---|---|---|---|
| Mut42-openmoc-runner-clamp-x-offset-positive | Mut44-openmc-runner-clamp-x-offset-positive | detected | missed |


## Threshold sensitivity

Re-classify candidates at tightened and relaxed relative thresholds using the matrix data;

how many flip relative to the 0.5% baseline?


| Relative threshold | # flips vs 0.5% baseline | Which |
|---|---|---|
| 0.20% | 0 | — |
| 0.50% (baseline) | 0 | — |
| 1.00% | 0 | — |
| 2.00% | 0 | — |

---

See [`discussion.md`](discussion.md) for hand-written analysis (coverage gaps, cross-solver κ interpretation, Phase 2 hand-off).

