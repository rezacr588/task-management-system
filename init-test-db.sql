-- Create test database if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT FROM pg_database WHERE datname = 'todoapi_test') THEN
        PERFORM dblink_exec('dbname=postgres', 'CREATE DATABASE todoapi_test');
    END IF;
END
$$;