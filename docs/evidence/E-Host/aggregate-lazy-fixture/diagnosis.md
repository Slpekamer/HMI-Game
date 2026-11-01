# Integration fixture assumption corrected

The 20:48 unified run passed Core, asset/import/smoke, all Godot host/presentation/UI probes, then failed before the sparse-control allocation assertion: the independent fixture directly opened control.json beneath a directory it assumed the PhysicalCheckpointFiles constructor had created. The unified adapter intentionally defers filesystem writes until AcquireWriter, so constructor failures can be handled in the normal persistence error flow.

Only the test was changed: assert the constructor does not create the directory, then explicitly create the fixture's own temporary directory before placing external corrupt input. The bounded-read test and all production validation are unchanged. Focused store rerun passed112, including sparse100MB input with3176 allocated bytes; session rerun passed144. Full aggregate must run again. This is not a production storage failure or grounds to weaken bounds.
