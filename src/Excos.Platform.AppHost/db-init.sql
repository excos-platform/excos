DO $$
BEGIN    IF NOT EXISTS(
        SELECT schema_name
          FROM information_schema.schemata
          WHERE schema_name = 'excos'
      )
    THEN
      EXECUTE 'CREATE SCHEMA excos';
    END IF;
 
      END
$$;